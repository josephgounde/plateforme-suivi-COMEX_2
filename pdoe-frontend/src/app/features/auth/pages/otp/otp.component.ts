// Vérification OTP entre le login LDAP et le dashboard. Pas d'état propre : tout vit dans AuthService, non persisté
// en storage — un refresh pendant la saisie renvoie donc au formulaire de connexion.

import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { AuthErrorCode, AuthService } from '../../../../core/auth/auth.service';
import { CanalNotification } from '../../../../core/models/enums.model';
import { environment } from '../../../../../environments/environment';

// Partial : cet écran ne reçoit que des codes OTP_*, jamais les codes LDAP.
const MESSAGES_ERREUR: Partial<Record<AuthErrorCode, string>> = {
  OTP_INVALID: 'Code incorrect.',
  OTP_EXPIRED: "Code expiré — merci d'en demander un nouveau.",
  OTP_MAX_TENTATIVES: 'Trop de tentatives échouées — merci de demander un nouveau code.'
};

@Component({
  selector: 'app-otp',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './otp.component.html',
  styleUrl: './otp.component.scss'
})
export class OtpComponent implements OnInit, OnDestroy {
  form: FormGroup;
  loading = false;
  erreur = '';

  // Affiché uniquement en mode mock — le code est fixe (123456) et
  // n'a aucune valeur à masquer hors d'un contexte de test.
  readonly modeMock = environment.useMockAuth;
  readonly codeMock = '123456';

  otpToken = '';
  canal: CanalNotification | null = null;
  destinataireMasque = '';

  secondesRestantes = 0;

  renvoiEnCours = false;
  renvoiSucces = '';
  cooldownRenvoi = 0;

  // true après OTP_MAX_TENTATIVES (ou OTP_EXPIRED sur un renvoi) : le otpToken est définitivement invalidé côté
  // backend, "Renvoyer" ne peut plus le ranimer — sans ce garde l'utilisateur restait bloqué sans comprendre pourquoi.
  sessionExpiree = false;

  private minuteurExpiration?: ReturnType<typeof setInterval>;
  private minuteurCooldown?: ReturnType<typeof setInterval>;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {
    this.form = this.fb.group({
      code: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]]
    });
  }

  ngOnInit(): void {
    const challenge = this.auth.challengeOtpEnCours;
    if (!challenge) {
      this.router.navigate(['/auth/login']);
      return;
    }

    this.otpToken = challenge.otpToken;
    this.canal = challenge.canal;
    this.destinataireMasque = challenge.destinataireMasque;
    this.demarrerCompteARebours(challenge.expiresInSeconds);
    this.demarrerCooldownRenvoi();
  }

  ngOnDestroy(): void {
    if (this.minuteurExpiration) clearInterval(this.minuteurExpiration);
    if (this.minuteurCooldown) clearInterval(this.minuteurCooldown);
  }

  get canalLabel(): string {
    switch (this.canal) {
      case CanalNotification.SMS: return 'SMS';
      case CanalNotification.SMS_ET_EMAIL: return 'SMS et e-mail';
      default: return 'e-mail';
    }
  }

  get expire(): boolean {
    return this.secondesRestantes <= 0;
  }

  get tempsFormate(): string {
    const m = Math.floor(this.secondesRestantes / 60);
    const s = this.secondesRestantes % 60;
    return `${m}:${s.toString().padStart(2, '0')}`;
  }

  soumettre(): void {
    if (this.form.invalid || this.loading || this.expire) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.erreur = '';

    this.auth.verifierOtp(this.otpToken, this.form.value.code).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: (err: HttpErrorResponse) => {
        const code = err.error?.code as AuthErrorCode | undefined;
        this.erreur = (code && MESSAGES_ERREUR[code]) ?? MESSAGES_ERREUR.OTP_INVALID!;
        this.loading = false;

        if (code === 'OTP_MAX_TENTATIVES') {
          this.sessionExpiree = true;
          this.erreur = 'Trop de tentatives échouées — merci de vous reconnecter.';
        }
        if (code === 'OTP_EXPIRED' || code === 'OTP_MAX_TENTATIVES') {
          this.secondesRestantes = 0;
          if (this.minuteurExpiration) clearInterval(this.minuteurExpiration);
        }
        this.cdr.detectChanges();
      }
    });
  }

  renvoyer(): void {
    if (this.renvoiEnCours || this.cooldownRenvoi > 0 || this.sessionExpiree) return;

    this.renvoiEnCours = true;
    this.erreur = '';
    this.renvoiSucces = '';

    this.auth.renvoyerOtp(this.otpToken).subscribe({
      next: challenge => {
        this.canal = challenge.canal;
        this.destinataireMasque = challenge.destinataireMasque;
        this.demarrerCompteARebours(challenge.expiresInSeconds);
        this.demarrerCooldownRenvoi();
        this.renvoiEnCours = false;
        this.renvoiSucces = 'Nouveau code envoyé.';
        this.form.reset();
        this.cdr.detectChanges();
        setTimeout(() => {
          this.renvoiSucces = '';
          this.cdr.detectChanges();
        }, 3000);
      },
      error: (err: HttpErrorResponse) => {
        this.renvoiEnCours = false;
        const code = err.error?.code as AuthErrorCode | undefined;
        if (code === 'OTP_EXPIRED') {
          this.sessionExpiree = true;
          this.erreur = 'Session de vérification expirée — merci de vous reconnecter.';
        } else {
          this.erreur = "Échec de l'envoi du nouveau code.";
        }
        this.cdr.detectChanges();
      }
    });
  }

  annuler(): void {
    this.auth.annulerOtp();
    this.router.navigate(['/auth/login']);
  }

  private demarrerCompteARebours(secondes: number): void {
    if (this.minuteurExpiration) clearInterval(this.minuteurExpiration);
    this.secondesRestantes = secondes;
    this.minuteurExpiration = setInterval(() => {
      this.secondesRestantes = Math.max(0, this.secondesRestantes - 1);
      if (this.secondesRestantes === 0 && this.minuteurExpiration) {
        clearInterval(this.minuteurExpiration);
      }
      this.cdr.detectChanges();
    }, 1000);
  }

  private demarrerCooldownRenvoi(): void {
    if (this.minuteurCooldown) clearInterval(this.minuteurCooldown);
    this.cooldownRenvoi = 30;
    this.minuteurCooldown = setInterval(() => {
      this.cooldownRenvoi = Math.max(0, this.cooldownRenvoi - 1);
      if (this.cooldownRenvoi === 0 && this.minuteurCooldown) {
        clearInterval(this.minuteurCooldown);
      }
      this.cdr.detectChanges();
    }, 1000);
  }
}
