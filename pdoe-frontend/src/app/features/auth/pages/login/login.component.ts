// Formulaire de connexion LDAP + bloc "connexion rapide" (dev only, logins à garder synchronisés avec MockUtilisateursStore).
// Un login valide déclenche l'OTP, pas directement le dashboard.

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { AuthErrorCode, AuthService } from '../../../../core/auth/auth.service';

interface QuickLoginOption {
  login: string;
  libelle: string;
}

// Un "Identifiants invalides" générique masquerait un compte verrouillé/expiré/désactivé, résolvable sans support DSIRI.
// INVALID_CREDENTIALS reste générique (ne jamais révéler si le login existe). Partial car cette page ne reçoit que des codes LDAP.
const MESSAGES_ERREUR: Partial<Record<AuthErrorCode, string>> = {
  INVALID_CREDENTIALS: 'Identifiants invalides.',
  ACCOUNT_LOCKED: 'Compte verrouillé après plusieurs tentatives échouées. Contactez le support DSIRI.',
  PASSWORD_EXPIRED: 'Mot de passe expiré — merci de le réinitialiser via le portail AD.',
  ACCOUNT_DISABLED: 'Compte désactivé. Contactez votre administrateur.',
  NO_PROFILE_MAPPED: "Aucun profil PDOE associé à ce compte. Contactez l'administrateur DSIRI.",
  LDAP_UNAVAILABLE: "Service d'authentification indisponible. Réessayez dans quelques instants."
};

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  form: FormGroup;
  loading = false;
  erreur = '';

  // Reflète le mapping profilParLogin de AuthService.
  readonly connexionsRapides: QuickLoginOption[] = [
    { login: 'agent.accueil', libelle: "Agent d'accueil" },
    { login: 'gestionnaire', libelle: 'Gestionnaire' },
    { login: 'comex', libelle: 'Agent COMEX' },
    { login: 'tresorerie', libelle: 'Trésorerie' },
    { login: 'direction', libelle: 'Direction' },
    { login: 'admin', libelle: 'Admin DSIRI' },
    { login: 'super.admin', libelle: 'Super Administrateur' }
  ];

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router
  ) {
    this.form = this.fb.group({
      login: ['', Validators.required],
      password: ['', Validators.required]
    });
  }

  connexionRapide(login: string): void {
    this.form.patchValue({ login, password: 'mock' });
    this.soumettre();
  }

  soumettre(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.erreur = '';
    const { login, password } = this.form.value;

    this.auth.login(login, password).subscribe({
      next: () => this.router.navigate(['/auth/otp']),
      error: (err: HttpErrorResponse) => {
        const code = err.error?.code as AuthErrorCode | undefined;
        this.erreur = (code && MESSAGES_ERREUR[code]) ?? MESSAGES_ERREUR.INVALID_CREDENTIALS!;
        this.loading = false;
      }
    });
  }
}