// Source de vérité mock restante pour /utilisateurs (pas de contrôleur .NET) et /auth/logout.
// Tout le reste a un vrai contrôleur pdoe-backend — voir mock.interceptor.ts (TOUJOURS_MOCKE).

import { Injectable } from '@angular/core';
import { HttpRequest } from '@angular/common/http';
import {
  Utilisateur,
  CreerUtilisateurRequest,
  ModifierUtilisateurRequest
} from '../models/dossier.model';
import { ProfilUtilisateur, CategorieAudit } from '../models/enums.model';
import { AuthService } from '../auth/auth.service';
import { MockUtilisateursStore } from './mock-utilisateurs.store';
import { MockJournalAuditStore } from './mock-journal-audit.store';

// Erreur simulant une réponse HTTP en échec — MockInterceptor la convertit en HttpErrorResponse.
export class MockHttpError extends Error {
  constructor(public readonly status: number, message: string) {
    super(message);
  }
}

@Injectable({ providedIn: 'root' })
export class MockDataService {
  constructor(
    private auth: AuthService,
    private utilisateursStore: MockUtilisateursStore,
    private journalAudit: MockJournalAuditStore
  ) {}

  // Routeur principal — appelé par MockInterceptor
  handleRequest(req: HttpRequest<unknown>): unknown | null {
    const url = req.url.replace(/.*\/api/, '');
    const method = req.method;

    // AuthController n'existe pas encore côté .NET, donc /auth/logout reste mocké même hors mode mock complet.
    if (method === 'POST' && url === '/auth/logout') return {};

    if (method === 'GET' && url === '/utilisateurs') return this.mockListeUtilisateurs();
    if (method === 'POST' && url === '/utilisateurs') return this.mockCreerUtilisateur(req);
    if (method === 'PATCH' && /\/utilisateurs\/\d+/.test(url)) return this.mockModifierUtilisateur(url, req);

    return null;
  }

  // Gestion des utilisateurs — annuaire local (Admin DSIRI)

  private exigerAdmin(): void {
    if (this.auth.profil !== ProfilUtilisateur.ADMIN_DSIRI && this.auth.profil !== ProfilUtilisateur.SUPER_ADMIN) {
      throw new MockHttpError(403, 'Réservé au profil Admin DSIRI ou Super Admin.');
    }
  }

  private mockListeUtilisateurs(): Utilisateur[] {
    this.exigerAdmin();
    return this.utilisateursStore.utilisateurs;
  }

  private mockCreerUtilisateur(req: HttpRequest<unknown>): Utilisateur {
    this.exigerAdmin();

    const body = (req.body ?? {}) as Partial<CreerUtilisateurRequest>;
    if (!body.loginAD?.trim() || !body.nom?.trim() || !body.prenom?.trim() || !body.email?.trim() || !body.profil) {
      throw new MockHttpError(400, 'Tous les champs sont requis.');
    }
    if (this.utilisateursStore.findByLogin(body.loginAD)) {
      throw new MockHttpError(409, 'Ce login AD est déjà utilisé.');
    }

    const maintenant = new Date().toISOString();
    const utilisateur: Utilisateur = {
      utilisateurId: this.utilisateursStore.prochainId++,
      loginAD: body.loginAD,
      nom: body.nom,
      prenom: body.prenom,
      email: body.email,
      profil: body.profil,
      estActif: true,
      createdAt: maintenant,
      updatedAt: maintenant,
      updatedBy: this.auth.currentUser?.login ?? 'admin'
    };
    this.utilisateursStore.utilisateurs.push(utilisateur);

    this.journalAudit.enregistrer({
      categorie: CategorieAudit.UTILISATEUR,
      typeAction: 'UTILISATEUR_CREE',
      description: `Création du compte ${utilisateur.loginAD} (${utilisateur.profil}).`,
      acteur: utilisateur.updatedBy,
      entiteType: 'Utilisateur',
      entiteId: String(utilisateur.utilisateurId)
    });

    return utilisateur;
  }

  private mockModifierUtilisateur(url: string, req: HttpRequest<unknown>): Utilisateur {
    this.exigerAdmin();

    const id = Number(url.match(/\/utilisateurs\/(\d+)/)?.[1] ?? NaN);
    const utilisateur = this.utilisateursStore.utilisateurs.find(u => u.utilisateurId === id);
    if (!utilisateur) throw new MockHttpError(404, 'Utilisateur introuvable.');

    const body = (req.body ?? {}) as ModifierUtilisateurRequest;

    // Un Admin ne peut pas se désactiver lui-même ni quitter le palier admin (évite un auto-verrouillage).
    // ADMIN_DSIRI/SUPER_ADMIN comptent tous deux comme "palier admin" — l'aller-retour entre les deux reste permis.
    const estSoiMeme = utilisateur.loginAD === this.auth.currentUser?.login;
    const estPalierAdmin = (p: ProfilUtilisateur) => p === ProfilUtilisateur.ADMIN_DSIRI || p === ProfilUtilisateur.SUPER_ADMIN;
    if (estSoiMeme && (body.estActif === false || (body.profil !== undefined && !estPalierAdmin(body.profil)))) {
      throw new MockHttpError(409, 'Impossible de retirer vos propres droits d\'administration.');
    }

    const estDesactivation = body.estActif === false && utilisateur.estActif === true;
    const estReactivation = body.estActif === true && utilisateur.estActif === false;

    if (body.nom !== undefined) utilisateur.nom = body.nom;
    if (body.prenom !== undefined) utilisateur.prenom = body.prenom;
    if (body.email !== undefined) utilisateur.email = body.email;
    if (body.profil !== undefined) utilisateur.profil = body.profil;
    if (body.estActif !== undefined) utilisateur.estActif = body.estActif;
    utilisateur.updatedAt = new Date().toISOString();
    utilisateur.updatedBy = this.auth.currentUser?.login ?? utilisateur.updatedBy;

    const typeAction = estDesactivation ? 'UTILISATEUR_DESACTIVE' : estReactivation ? 'UTILISATEUR_REACTIVE' : 'UTILISATEUR_MODIFIE';
    const libelleAction = estDesactivation ? 'Désactivation' : estReactivation ? 'Réactivation' : 'Modification';
    this.journalAudit.enregistrer({
      categorie: CategorieAudit.UTILISATEUR,
      typeAction,
      description: `${libelleAction} du compte ${utilisateur.loginAD}.`,
      acteur: utilisateur.updatedBy,
      entiteType: 'Utilisateur',
      entiteId: String(utilisateur.utilisateurId)
    });

    return utilisateur;
  }
}
