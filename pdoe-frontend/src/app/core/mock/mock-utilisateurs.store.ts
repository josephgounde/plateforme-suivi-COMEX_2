// Annuaire local des utilisateurs (mock), reflète dbo.Utilisateurs.
// Séparé de MockDataService pour éviter une dépendance circulaire avec AuthService.

import { Injectable } from '@angular/core';
import { Utilisateur } from '../models/dossier.model';
import { ProfilUtilisateur } from '../models/enums.model';

const SEED_DATE = '2025-01-06T08:00:00.000Z';

@Injectable({ providedIn: 'root' })
export class MockUtilisateursStore {
  // Public et mutable : ce store EST la table, pas une façade en lecture seule.
  utilisateurs: Utilisateur[] = [
    { utilisateurId: 1, loginAD: 'agent.accueil', nom: 'Konan', prenom: 'Adjoua', email: 'agent.accueil@afbci.ci', profil: ProfilUtilisateur.AGENT_ACCUEIL, estActif: true, createdAt: SEED_DATE, updatedAt: SEED_DATE, updatedBy: 'SYSTEM' },
    { utilisateurId: 2, loginAD: 'gestionnaire', nom: 'Traoré', prenom: 'Ismaël', email: 'gestionnaire@afbci.ci', profil: ProfilUtilisateur.GESTIONNAIRE, estActif: true, createdAt: SEED_DATE, updatedAt: SEED_DATE, updatedBy: 'SYSTEM' },
    { utilisateurId: 3, loginAD: 'gestionnaire.diallo', nom: 'Diallo', prenom: 'Fatoumata', email: 'gestionnaire.diallo@afbci.ci', profil: ProfilUtilisateur.GESTIONNAIRE, estActif: true, createdAt: SEED_DATE, updatedAt: SEED_DATE, updatedBy: 'SYSTEM' },
    { utilisateurId: 4, loginAD: 'gestionnaire.kone', nom: 'Koné', prenom: 'Yves', email: 'gestionnaire.kone@afbci.ci', profil: ProfilUtilisateur.GESTIONNAIRE, estActif: true, createdAt: SEED_DATE, updatedAt: SEED_DATE, updatedBy: 'SYSTEM' },
    { utilisateurId: 5, loginAD: 'comex', nom: 'Bamba', prenom: 'Serge', email: 'comex@afbci.ci', profil: ProfilUtilisateur.AGENT_COMEX, estActif: true, createdAt: SEED_DATE, updatedAt: SEED_DATE, updatedBy: 'SYSTEM' },
    { utilisateurId: 6, loginAD: 'tresorerie', nom: 'Kouassi', prenom: 'Michelle', email: 'tresorerie@afbci.ci', profil: ProfilUtilisateur.TRESORERIE, estActif: true, createdAt: SEED_DATE, updatedAt: SEED_DATE, updatedBy: 'SYSTEM' },
    { utilisateurId: 7, loginAD: 'direction', nom: "N'Guessan", prenom: 'Paul', email: 'direction@afbci.ci', profil: ProfilUtilisateur.DIRECTION, estActif: true, createdAt: SEED_DATE, updatedAt: SEED_DATE, updatedBy: 'SYSTEM' },
    { utilisateurId: 8, loginAD: 'admin', nom: 'Joseph', prenom: 'Admin', email: 'admin@afbci.ci', profil: ProfilUtilisateur.ADMIN_DSIRI, estActif: true, createdAt: SEED_DATE, updatedAt: SEED_DATE, updatedBy: 'SYSTEM' },
    // Compte désactivé de démonstration — permet d'exercer ACCOUNT_DISABLED à la connexion.
    { utilisateurId: 9, loginAD: 'ex.stagiaire', nom: 'Kacou', prenom: 'Ange', email: 'ex.stagiaire@afbci.ci', profil: ProfilUtilisateur.AGENT_ACCUEIL, estActif: false, createdAt: SEED_DATE, updatedAt: SEED_DATE, updatedBy: 'SYSTEM' },
    { utilisateurId: 10, loginAD: 'super.admin', nom: 'Gounde', prenom: 'Joseph', email: 'super.admin@afbci.ci', profil: ProfilUtilisateur.SUPER_ADMIN, estActif: true, createdAt: SEED_DATE, updatedAt: SEED_DATE, updatedBy: 'SYSTEM' }
  ];

  prochainId = 11;

  findByLogin(login: string): Utilisateur | undefined {
    return this.utilisateurs.find(u => u.loginAD === login);
  }
}
