// Authentification LDAP en prod (via PDOE.Gateway), simulée en mode mock en
// mappant le login à un profil PDOE. Jamais de mot de passe stocké, juste le JWT.

import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { UtilisateurConnecte, Utilisateur, OtpChallenge, VerifierOtpRequest } from '../models/dossier.model';
import { ProfilUtilisateur, CanalNotification, CategorieAudit } from '../models/enums.model';
import { MockUtilisateursStore } from '../mock/mock-utilisateurs.store';
import { MockJournalAuditStore } from '../mock/mock-journal-audit.store';

const TOKEN_KEY = 'pdoe_token';
const USER_KEY = 'pdoe_user';

// Codes d'erreur de POST /auth/login (bind LDAP échoué) — à resynchroniser avec le backend une fois PDOE.Gateway implémenté.
// Les codes OTP_* ne surviennent que sur /auth/otp/verifier ; un seul type couvre les deux endpoints (même format d'erreur).
export type AuthErrorCode =
  | 'INVALID_CREDENTIALS'
  | 'ACCOUNT_LOCKED'
  | 'PASSWORD_EXPIRED'
  | 'ACCOUNT_DISABLED'
  | 'NO_PROFILE_MAPPED'
  | 'LDAP_UNAVAILABLE'
  | 'OTP_INVALID'
  | 'OTP_EXPIRED'
  | 'OTP_MAX_TENTATIVES';

// Durée de validité et nombre d'essais avant blocage — cohérent avec
// les dispositifs OTP bancaires usuels (code valable 3 min, 3 essais).
const OTP_VALIDITE_SECONDES = 180;
const OTP_MAX_TENTATIVES = 3;

// Code fixe accepté en mode mock quel que soit l'utilisateur — affiché en clair sur l'écran OTP.
const OTP_CODE_MOCK = '123456';

interface OtpEnAttente {
  utilisateur: Utilisateur;
  tentatives: number;
  expiresAt: number;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  // Minuteur d'expiration proactive — doit être déclaré AVANT currentUserSubject (ES2022 class fields,
  // sinon écrase le setTimeout posé par initUserFromStorage() pendant l'init de currentUserSubject).
  private expiryTimer?: ReturnType<typeof setTimeout>;

  private currentUserSubject = new BehaviorSubject<UtilisateurConnecte | null>(
    this.initUserFromStorage()
  );

  // Consommé par les composants qui doivent réagir à un changement de session.
  currentUser$: Observable<UtilisateurConnecte | null> = this.currentUserSubject.asObservable();

  // Jamais persisté : un refresh pendant la saisie OTP doit renvoyer au login, pas laisser un écran orphelin.
  private otpEnAttente: OtpEnAttente | null = null;
  private challengeActuel: OtpChallenge | null = null;

  constructor(
    private http: HttpClient,
    private router: Router,
    private annuaire: MockUtilisateursStore,
    private journalAudit: MockJournalAuditStore
  ) {}

  // Étape 1/2 — vérifie les identifiants (bind LDAP), déclenche l'envoi OTP plutôt que délivrer une session directe.
  // En mode mock, résout le profil via MockUtilisateursStore sans vérifier le mot de passe (sauf logins "test.*").
  login(login: string, password: string): Observable<OtpChallenge> {
    if (environment.useMockAuth) {
      const erreurSimulee = this.erreurMockPourLogin(login);
      if (erreurSimulee) {
        this.journalAudit.enregistrer({
          categorie: CategorieAudit.AUTHENTIFICATION,
          typeAction: 'CONNEXION_ECHEC',
          description: `Échec de connexion pour "${login}" (${erreurSimulee.code}).`,
          acteur: login,
          succes: false
        });
        return throwError(() => new HttpErrorResponse({
          status: erreurSimulee.status,
          error: { code: erreurSimulee.code }
        }));
      }

      const utilisateur = this.annuaire.findByLogin(login);
      if (!utilisateur) {
        this.journalAudit.enregistrer({
          categorie: CategorieAudit.AUTHENTIFICATION,
          typeAction: 'CONNEXION_ECHEC',
          description: `Échec de connexion pour "${login}" (aucun profil PDOE associé).`,
          acteur: login,
          succes: false
        });
        return throwError(() => new HttpErrorResponse({
          status: 403,
          error: { code: 'NO_PROFILE_MAPPED' as AuthErrorCode }
        }));
      }
      if (!utilisateur.estActif) {
        this.journalAudit.enregistrer({
          categorie: CategorieAudit.AUTHENTIFICATION,
          typeAction: 'CONNEXION_ECHEC',
          description: `Échec de connexion pour "${login}" (compte désactivé).`,
          acteur: login,
          succes: false
        });
        return throwError(() => new HttpErrorResponse({
          status: 403,
          error: { code: 'ACCOUNT_DISABLED' as AuthErrorCode }
        }));
      }

      return of(this.demarrerOtpMock(utilisateur));
    }

    return this.http
      .post<OtpChallenge>(`${environment.apiUrl}/auth/login`, { login, password })
      .pipe(tap(challenge => { this.challengeActuel = challenge; }));
  }

  // Étape 2/2 — délivre la session réelle (JWT + métadonnées) si le code est valide. Aucune session avant cet appel.
  verifierOtp(otpToken: string, code: string): Observable<UtilisateurConnecte> {
    if (environment.useMockAuth) {
      return this.verifierOtpMock(otpToken, code);
    }

    return this.http
      .post<UtilisateurConnecte>(`${environment.apiUrl}/auth/otp/verifier`, { otpToken, code } as VerifierOtpRequest)
      .pipe(tap(user => {
        this.challengeActuel = null;
        this.persistSession(user);
      }));
  }

  // Renvoie un code — côté mock, seuls l'horloge d'expiration et le compteur de tentatives sont réinitialisés.
  renvoyerOtp(otpToken: string): Observable<OtpChallenge> {
    if (environment.useMockAuth) {
      if (!this.otpEnAttente || this.challengeActuel?.otpToken !== otpToken) {
        return throwError(() => new HttpErrorResponse({
          status: 410,
          error: { code: 'OTP_EXPIRED' as AuthErrorCode }
        }));
      }

      this.otpEnAttente.tentatives = 0;
      this.otpEnAttente.expiresAt = Date.now() + OTP_VALIDITE_SECONDES * 1000;
      const challenge = this.construireChallenge(otpToken, this.otpEnAttente.utilisateur);
      this.challengeActuel = challenge;
      return of(challenge);
    }

    return this.http
      .post<OtpChallenge>(`${environment.apiUrl}/auth/otp/renvoyer`, { otpToken })
      .pipe(tap(challenge => { this.challengeActuel = challenge; }));
  }

  // Consulté par OtpComponent au démarrage — si null (accès direct/refresh), renvoie vers /auth/login.
  get challengeOtpEnCours(): OtpChallenge | null {
    return this.challengeActuel;
  }

  // Bouton "Retour" de l'écran OTP — purge l'état transitoire, rien à révoquer côté serveur (pas de session créée).
  annulerOtp(): void {
    this.otpEnAttente = null;
    this.challengeActuel = null;
  }

  // Purge la session locale immédiatement puis notifie le backend en best-effort pour révoquer le token serveur.
  logout(): void {
    const token = this.token;
    // Capturé AVANT purgerSession() : currentUser redevient null dès cet appel.
    const acteur = this.currentUser?.login;
    if (acteur) {
      this.journalAudit.enregistrer({
        categorie: CategorieAudit.AUTHENTIFICATION,
        typeAction: 'DECONNEXION',
        description: `Déconnexion — ${acteur}.`,
        acteur
      });
    }
    this.purgerSession();
    this.router.navigate(['/auth/login']);

    if (token) {
      this.http
        .post(`${environment.apiUrl}/auth/logout`, {}, { headers: { Authorization: `Bearer ${token}` } })
        .subscribe({ error: () => {} });
    }
  }

  get currentUser(): UtilisateurConnecte | null {
    return this.currentUserSubject.value;
  }

  get token(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  get isAuthenticated(): boolean {
    const user = this.currentUser;
    if (!user) return false;

    if (this.estExpire(user.expiresAt)) {
      // Purge tardive (ex: minuteur non déclenché, onglet en arrière-plan) pour éviter un utilisateur fantôme.
      this.purgerSession();
      return false;
    }

    return true;
  }

  get profil(): string {
    return this.currentUser?.profil ?? '';
  }

  // Vérifie si le profil courant fait partie des rôles autorisés.
  // Utilisé par AuthGuard pour les routes restreintes (ex: /admin).
  hasRole(...roles: ProfilUtilisateur[]): boolean {
    return roles.some(role => role === this.profil);
  }

  // Implémentation interne

  private persistSession(user: UtilisateurConnecte): void {
    localStorage.setItem(TOKEN_KEY, user.token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this.currentUserSubject.next(user);
    this.planifierExpiration(user.expiresAt);
  }

  private purgerSession(): void {
    this.clearTimer();
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.currentUserSubject.next(null);
  }

  private estExpire(expiresAt: string): boolean {
    return new Date(expiresAt).getTime() <= Date.now();
  }

  // Déconnexion automatique à l'expiration du token, plutôt que d'attendre le prochain 401.
  private planifierExpiration(expiresAt: string): void {
    this.clearTimer();
    const delai = new Date(expiresAt).getTime() - Date.now();
    if (delai <= 0) {
      this.logout();
      return;
    }
    this.expiryTimer = setTimeout(() => this.logout(), delai);
  }

  private clearTimer(): void {
    if (this.expiryTimer) {
      clearTimeout(this.expiryTimer);
      this.expiryTimer = undefined;
    }
  }

  private initUserFromStorage(): UtilisateurConnecte | null {
    const user = this.readUserFromStorage();
    if (!user) return null;

    if (this.estExpire(user.expiresAt)) {
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
      return null;
    }

    this.planifierExpiration(user.expiresAt);
    return user;
  }

  private readUserFromStorage(): UtilisateurConnecte | null {
    try {
      const raw = localStorage.getItem(USER_KEY);
      return raw ? (JSON.parse(raw) as UtilisateurConnecte) : null;
    } catch {
      return null;
    }
  }

  // Logins de test (préfixe "test.") pour exercer les messages d'erreur AD sans backend réel.
  // Distincts des logins "connexion rapide" (agent.accueil, gestionnaire...) qui simulent un succès.
  private erreurMockPourLogin(login: string): { status: number; code: AuthErrorCode } | null {
    const table: Record<string, AuthErrorCode> = {
      'test.locked': 'ACCOUNT_LOCKED',
      'test.expired': 'PASSWORD_EXPIRED',
      'test.disabled': 'ACCOUNT_DISABLED',
      'test.nomapping': 'NO_PROFILE_MAPPED',
      'test.ldapdown': 'LDAP_UNAVAILABLE',
      'test.invalid': 'INVALID_CREDENTIALS'
    };
    const code = table[login];
    if (!code) return null;

    return { status: code === 'LDAP_UNAVAILABLE' ? 503 : code === 'INVALID_CREDENTIALS' ? 401 : 403, code };
  }

  // Profil/nom/email viennent de MockUtilisateursStore, pas d'un mapping figé — gestionnaire.diallo/kone
  // restent deux identités distinctes pour vérifier l'isolation par portefeuille.
  private buildMockUser(utilisateur: Utilisateur): UtilisateurConnecte {
    return {
      login: utilisateur.loginAD,
      nomComplet: `${utilisateur.prenom} ${utilisateur.nom}`,
      email: utilisateur.email,
      profil: utilisateur.profil,
      token: `mock-jwt-${utilisateur.loginAD}-${Date.now()}`,
      expiresAt: new Date(Date.now() + 8 * 3600 * 1000).toISOString()
    };
  }

  // Après bind LDAP mock réussi — n'accorde encore aucune session.
  private demarrerOtpMock(utilisateur: Utilisateur): OtpChallenge {
    const otpToken = `otp-${utilisateur.loginAD}-${Date.now()}`;
    this.otpEnAttente = {
      utilisateur,
      tentatives: 0,
      expiresAt: Date.now() + OTP_VALIDITE_SECONDES * 1000
    };
    const challenge = this.construireChallenge(otpToken, utilisateur);
    this.challengeActuel = challenge;
    return challenge;
  }

  private verifierOtpMock(otpToken: string, code: string): Observable<UtilisateurConnecte> {
    const attente = this.otpEnAttente;
    if (!attente || this.challengeActuel?.otpToken !== otpToken) {
      return throwError(() => new HttpErrorResponse({
        status: 410,
        error: { code: 'OTP_EXPIRED' as AuthErrorCode }
      }));
    }

    if (Date.now() > attente.expiresAt) {
      this.otpEnAttente = null;
      this.challengeActuel = null;
      return throwError(() => new HttpErrorResponse({
        status: 410,
        error: { code: 'OTP_EXPIRED' as AuthErrorCode }
      }));
    }

    if (code !== OTP_CODE_MOCK) {
      attente.tentatives++;
      if (attente.tentatives >= OTP_MAX_TENTATIVES) {
        this.otpEnAttente = null;
        this.challengeActuel = null;
        return throwError(() => new HttpErrorResponse({
          status: 429,
          error: { code: 'OTP_MAX_TENTATIVES' as AuthErrorCode }
        }));
      }
      return throwError(() => new HttpErrorResponse({
        status: 401,
        error: { code: 'OTP_INVALID' as AuthErrorCode }
      }));
    }

    const user = this.buildMockUser(attente.utilisateur);
    this.otpEnAttente = null;
    this.challengeActuel = null;
    this.persistSession(user);
    this.journalAudit.enregistrer({
      categorie: CategorieAudit.AUTHENTIFICATION,
      typeAction: 'CONNEXION_REUSSIE',
      description: `Connexion réussie — ${user.nomComplet} (${user.profil}).`,
      acteur: user.login
    });
    return of(user);
  }

  private construireChallenge(otpToken: string, utilisateur: Utilisateur): OtpChallenge {
    return {
      otpToken,
      canal: CanalNotification.EMAIL,
      destinataireMasque: this.masquerEmail(utilisateur.email),
      expiresInSeconds: OTP_VALIDITE_SECONDES
    };
  }

  // Masque tout sauf le 1er caractère de la partie locale — ex. "agent.accueil@afbci.ci" → "a**********@afbci.ci".
  private masquerEmail(email: string): string {
    const [local, domaine] = email.split('@');
    if (!domaine) return email;
    const visible = local.slice(0, 1);
    return `${visible}${'*'.repeat(Math.max(local.length - 1, 3))}@${domaine}`;
  }
}