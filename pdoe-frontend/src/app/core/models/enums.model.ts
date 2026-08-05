// Énumérations (PDOE_openapi.yaml, components/schemas).

// Type d'opération COMEX — chaque valeur a son propre délai d'apurement et justificatifs (cf. regles-apurement.model.ts).
export enum TypeOperation {
  IMPORT_BIENS = 'IMPORT_BIENS',
  IMPORT_SERVICES = 'IMPORT_SERVICES',
  EXPORT_BIENS = 'EXPORT_BIENS',
  EXPORT_SERVICES = 'EXPORT_SERVICES',
  TRANSFERT_CAPITAUX = 'TRANSFERT_CAPITAUX'
}

// ── Type de compte débité à l'initiation ──────────────────────
export enum TypeCompte {
  COURANT = 'COURANT',
  EPARGNE = 'EPARGNE',
  DEVISE = 'DEVISE'
}

// ── Qualité de la résidence du client (réglementation des changes BCEAO) ──
export enum QualiteResidence {
  RESIDENT = 'RESIDENT',
  NON_RESIDENT = 'NON_RESIDENT'
}

// ── Statut électronique du dossier (22 valeurs — cycle de vie complet) ──
export enum StatutDossier {
  BROUILLON = 'BROUILLON',
  INITIE = 'INITIE',
  EN_VALIDATION_GESTIONNAIRE = 'EN_VALIDATION_GESTIONNAIRE',
  ANTI_FRACTIONNEMENT_DETECTE = 'ANTI_FRACTIONNEMENT_DETECTE',
  CONFIRME_GESTIONNAIRE = 'CONFIRME_GESTIONNAIRE',
  EN_CONTROLE_COMEX = 'EN_CONTROLE_COMEX',
  VALIDE_COMEX = 'VALIDE_COMEX',
  EN_AVIS_TRESORERIE = 'EN_AVIS_TRESORERIE',
  AVIS_TRESORERIE_DONNE = 'AVIS_TRESORERIE_DONNE',
  EN_ATTENTE_EXECUTION = 'EN_ATTENTE_EXECUTION',
  EN_EXECUTION_SWIFT = 'EN_EXECUTION_SWIFT',
  EXECUTE = 'EXECUTE',
  EN_APUREMENT = 'EN_APUREMENT',
  APUREMENT_PARTIEL = 'APUREMENT_PARTIEL',
  ALERTE_J14 = 'ALERTE_J14',
  ALERTE_J8 = 'ALERTE_J8',
  DEPASSE_BCEAO = 'DEPASSE_BCEAO',
  APURE = 'APURE',
  EN_ARCHIVAGE = 'EN_ARCHIVAGE',
  ARCHIVE = 'ARCHIVE',
  REJETE_DEFINITIF = 'REJETE_DEFINITIF'
}

// ── Niveau de validation — les 7 étapes historiques. Pas l'ensemble fermé
// des codes possibles : une étape personnalisée (EtapeWorkflowConfig) n'y figure pas.
export enum NiveauValidation {
  ETAPE_1_INITIATION = 'ETAPE_1_INITIATION',
  ETAPE_2_GESTIONNAIRE = 'ETAPE_2_GESTIONNAIRE',
  ETAPE_3_COMEX = 'ETAPE_3_COMEX',
  ETAPE_4_TRESORERIE = 'ETAPE_4_TRESORERIE',
  ETAPE_5_EXECUTION = 'ETAPE_5_EXECUTION',
  ETAPE_6_APUREMENT = 'ETAPE_6_APUREMENT',
  ETAPE_7_ARCHIVAGE = 'ETAPE_7_ARCHIVAGE'
}

// ── Type d'étape configurable — détermine l'écran cible ────────
export enum TypeEtapeWorkflow {
  GESTIONNAIRE = 'GESTIONNAIRE',
  COMEX = 'COMEX',
  TRESORERIE = 'TRESORERIE',
  EXECUTION = 'EXECUTION',
  APUREMENT = 'APUREMENT',
  GENERIQUE = 'GENERIQUE'
}

// Sous-état d'un dossier sur une étape GENERIQUE (personnalisée) — remplace StatutDossier pour ces étapes-là.
export enum SousEtat {
  EN_ATTENTE = 'EN_ATTENTE',
  VALIDE = 'VALIDE',
  REJETE = 'REJETE'
}

// ── Action enregistrée dans l'historique workflow (EtapesWorkflow) ──
export enum ActionWorkflow {
  SOUMISSION = 'SOUMISSION',
  VALIDATION = 'VALIDATION',
  REJET = 'REJET',
  CORRECTION = 'CORRECTION',
  BASCULE_SWIFT = 'BASCULE_SWIFT',
  DECLARATION_EXECUTION = 'DECLARATION_EXECUTION',
  RECEPTION_JUSTIFICATIF = 'RECEPTION_JUSTIFICATIF',
  SIGNALEMENT_FRACTIONNEMENT = 'SIGNALEMENT_FRACTIONNEMENT',
  LEVEE_ALERTE = 'LEVEE_ALERTE',
  ESCALADE = 'ESCALADE',
  ARCHIVAGE = 'ARCHIVAGE'
}

// ── Type de document / pièce justificative ───────────────────
export enum TypeDocument {
  PIECE_IDENTITE = 'PIECE_IDENTITE',
  FACTURE_PROFORMA = 'FACTURE_PROFORMA',
  CONTRAT = 'CONTRAT',
  FDI = 'FDI',                             // Fiche de Déclaration d'Importation
  AUTORISATION_CHANGE = 'AUTORISATION_CHANGE',
  BAE = 'BAE',                             // Bon d'Apurement à l'Export
  D3 = 'D3',                               // Déclaration Douanière
  FACTURE_SOLDEE = 'FACTURE_SOLDEE',
  JUSTIFICATIF_APUREMENT = 'JUSTIFICATIF_APUREMENT',
  ORDRE_TRANSFERT = 'ORDRE_TRANSFERT',
  FORMULAIRE_CHANGE = 'FORMULAIRE_CHANGE',
  ATTESTATION_CHANGE = 'ATTESTATION_CHANGE',
  ATTESTATION_BNC = 'ATTESTATION_BNC',
  FACTURE_DEFINITIVE = 'FACTURE_DEFINITIVE',
  AUTRE = 'AUTRE'
}

// ── Type d'alerte réglementaire d'apurement ───────────────────
export enum TypeAlerte {
  RELANCE_J14 = 'RELANCE_J14',
  MISE_EN_DEMEURE_J8 = 'MISE_EN_DEMEURE_J8',
  DEPASSEMENT_J0 = 'DEPASSEMENT_J0',
  ESCALADE_DELAI = 'ESCALADE_DELAI',
  FRACTIONNEMENT = 'FRACTIONNEMENT'
}

// ── Canal de notification ─────────────────────────────────────
export enum CanalNotification {
  SMS = 'SMS',
  EMAIL = 'EMAIL',
  SMS_ET_EMAIL = 'SMS_ET_EMAIL'
}

// ── Statut d'envoi d'une notification ─────────────────────────
export enum StatutNotification {
  EN_ATTENTE = 'EN_ATTENTE',
  ENVOYE = 'ENVOYE',
  ECHEC = 'ECHEC',
  ECHEC_DEFINITIF = 'ECHEC_DEFINITIF'
}

// ── Catégorie d'une entrée du journal d'audit (actions admin/sécurité, pas le cycle de vie des dossiers) ──
export enum CategorieAudit {
  AUTHENTIFICATION = 'AUTHENTIFICATION',
  UTILISATEUR = 'UTILISATEUR',
  PARAMETRAGE = 'PARAMETRAGE',
  WORKFLOW = 'WORKFLOW',
  REPORTING = 'REPORTING'
}

// ── Journal des exports (GET /reporting/exports) — réglementaire (gabarits officiels DGI/Trésor/BCEAO) vs opérationnel (interne) ──
export enum CategorieExport {
  REGLEMENTAIRE = 'REGLEMENTAIRE',
  OPERATIONNEL = 'OPERATIONNEL'
}

export enum TypeExport {
  CRPI_DGI = 'CRPI_DGI',
  CRPI_TRESOR = 'CRPI_TRESOR',
  SITUATION_BCEAO = 'SITUATION_BCEAO',
  DOSSIERS_EN_RETARD = 'DOSSIERS_EN_RETARD',
  ACTIVITE_MENSUELLE = 'ACTIVITE_MENSUELLE',
  FICHE_DOSSIER = 'FICHE_DOSSIER',
  HISTORIQUE_DOSSIER = 'HISTORIQUE_DOSSIER'
}

// ── Mode de vérification de la signature ABS2000 (v5.2) ───────
export enum ModeVerificationSignature {
  AUTOMATIQUE = 'AUTOMATIQUE',   // Contrôle binaire ABS2000 — aucune intervention humaine
  VISUEL = 'VISUEL',             // Image affichée — agent compare et coche une case + initiales
  LES_DEUX = 'LES_DEUX'          // Automatique ET visuelle obligatoires
}

// ── Profils utilisateurs PDOE (acteurs internes) ──────────────
export enum ProfilUtilisateur {
  AGENT_ACCUEIL = 'AGENT_ACCUEIL',
  GESTIONNAIRE = 'GESTIONNAIRE',
  AGENT_COMEX = 'AGENT_COMEX',
  TRESORERIE = 'TRESORERIE',
  DIRECTION = 'DIRECTION',
  ADMIN_DSIRI = 'ADMIN_DSIRI',
  // Superset d'ADMIN_DSIRI + accès exclusif au Journal d'audit (séparation des tâches).
  SUPER_ADMIN = 'SUPER_ADMIN'
}

// ── LIBELLÉS — affichage en français dans l'interface ──

export const STATUT_LABELS: Record<StatutDossier, string> = {
  [StatutDossier.BROUILLON]: 'Brouillon',
  [StatutDossier.INITIE]: 'Initié',
  [StatutDossier.EN_VALIDATION_GESTIONNAIRE]: 'Validation gestionnaire',
  [StatutDossier.ANTI_FRACTIONNEMENT_DETECTE]: 'Alerte fractionnement',
  [StatutDossier.CONFIRME_GESTIONNAIRE]: 'Validé gestionnaire',
  [StatutDossier.EN_CONTROLE_COMEX]: 'Contrôle COMEX',
  [StatutDossier.VALIDE_COMEX]: 'Validé COMEX',
  [StatutDossier.EN_AVIS_TRESORERIE]: 'Avis Trésorerie',
  [StatutDossier.AVIS_TRESORERIE_DONNE]: 'Avis Trésorerie donné',
  [StatutDossier.EN_ATTENTE_EXECUTION]: 'Prêt pour exécution',
  [StatutDossier.EN_EXECUTION_SWIFT]: 'En exécution SWIFT',
  [StatutDossier.EXECUTE]: 'Exécuté',
  [StatutDossier.EN_APUREMENT]: 'En apurement',
  [StatutDossier.APUREMENT_PARTIEL]: 'Apurement partiel',
  [StatutDossier.ALERTE_J14]: 'Alerte J-14',
  [StatutDossier.ALERTE_J8]: 'Mise en demeure J-8',
  [StatutDossier.DEPASSE_BCEAO]: 'Dépassement BCEAO',
  [StatutDossier.APURE]: 'Apuré',
  [StatutDossier.EN_ARCHIVAGE]: 'En archivage',
  [StatutDossier.ARCHIVE]: 'Archivé',
  [StatutDossier.REJETE_DEFINITIF]: 'Rejeté définitivement'
};

export const TYPE_OPERATION_LABELS: Record<TypeOperation, string> = {
  [TypeOperation.IMPORT_BIENS]: 'Import de biens',
  [TypeOperation.IMPORT_SERVICES]: 'Import de services',
  [TypeOperation.EXPORT_BIENS]: 'Export de biens',
  [TypeOperation.EXPORT_SERVICES]: 'Export de services',
  [TypeOperation.TRANSFERT_CAPITAUX]: 'Transfert de revenus & capitaux'
};

export const QUALITE_RESIDENCE_LABELS: Record<QualiteResidence, string> = {
  [QualiteResidence.RESIDENT]: 'Résident',
  [QualiteResidence.NON_RESIDENT]: 'Non-résident'
};

export const TYPE_COMPTE_LABELS: Record<TypeCompte, string> = {
  [TypeCompte.COURANT]: 'Compte courant',
  [TypeCompte.EPARGNE]: 'Compte épargne',
  [TypeCompte.DEVISE]: 'Compte devise'
};

export const TYPE_DOCUMENT_LABELS: Record<TypeDocument, string> = {
  [TypeDocument.PIECE_IDENTITE]: "Pièce d'identité",
  [TypeDocument.FACTURE_PROFORMA]: 'Facture proforma',
  [TypeDocument.CONTRAT]: 'Contrat',
  [TypeDocument.FDI]: "Fiche de Déclaration d'Importation (FDI)",
  [TypeDocument.AUTORISATION_CHANGE]: 'Autorisation de change',
  [TypeDocument.BAE]: "Bon d'Apurement à l'Export (BAE)",
  [TypeDocument.D3]: 'Déclaration douanière (D3)',
  [TypeDocument.FACTURE_SOLDEE]: 'Facture soldée',
  [TypeDocument.JUSTIFICATIF_APUREMENT]: "Justificatif d'apurement",
  [TypeDocument.ORDRE_TRANSFERT]: 'Ordre de transfert',
  [TypeDocument.FORMULAIRE_CHANGE]: 'Formulaire de change',
  [TypeDocument.ATTESTATION_CHANGE]: 'Attestation de change (AC)',
  [TypeDocument.ATTESTATION_BNC]: 'Attestation BNC',
  [TypeDocument.FACTURE_DEFINITIVE]: 'Facture définitive',
  [TypeDocument.AUTRE]: 'Autre document'
};

// Placeholders d'exemple par type de document. N° d'AC et Code TRF déplacés vers Dossier.numeroAC/codeTRF (saisis par COMEX à l'exécution).
const REFERENCE_DOCUMENT_PLACEHOLDERS: Partial<Record<TypeDocument, string>> = {
  [TypeDocument.FACTURE_PROFORMA]: 'N° de référence (ex : n° de facture)',
  [TypeDocument.FACTURE_DEFINITIVE]: 'N° de référence (ex : n° de facture)'
};

export function referenceDocumentPlaceholder(type: TypeDocument): string {
  return REFERENCE_DOCUMENT_PLACEHOLDERS[type] ?? 'N° de référence (facultatif)';
}

export const MODE_VERIFICATION_LABELS: Record<ModeVerificationSignature, string> = {
  [ModeVerificationSignature.AUTOMATIQUE]: 'Automatique',
  [ModeVerificationSignature.VISUEL]: 'Visuel',
  [ModeVerificationSignature.LES_DEUX]: 'Automatique + Visuel'
};

export const PROFIL_LABELS: Record<ProfilUtilisateur, string> = {
  [ProfilUtilisateur.AGENT_ACCUEIL]: "Agent d'accueil",
  [ProfilUtilisateur.GESTIONNAIRE]: 'Gestionnaire de compte',
  [ProfilUtilisateur.AGENT_COMEX]: 'Agent COMEX',
  [ProfilUtilisateur.TRESORERIE]: 'Trésorerie',
  [ProfilUtilisateur.DIRECTION]: 'Direction / Supervision',
  [ProfilUtilisateur.ADMIN_DSIRI]: 'Administrateur DSIRI',
  [ProfilUtilisateur.SUPER_ADMIN]: 'Super Administrateur'
};

export const CATEGORIE_AUDIT_LABELS: Record<CategorieAudit, string> = {
  [CategorieAudit.AUTHENTIFICATION]: 'Authentification',
  [CategorieAudit.UTILISATEUR]: 'Utilisateurs',
  [CategorieAudit.PARAMETRAGE]: 'Paramétrage',
  [CategorieAudit.WORKFLOW]: 'Circuit',
  [CategorieAudit.REPORTING]: 'Exports réglementaires'
};

export const CATEGORIE_EXPORT_LABELS: Record<CategorieExport, string> = {
  [CategorieExport.REGLEMENTAIRE]: 'Réglementaire',
  [CategorieExport.OPERATIONNEL]: 'Opérationnel'
};

export const TYPE_EXPORT_LABELS: Record<TypeExport, string> = {
  [TypeExport.CRPI_DGI]: 'CRPI — Direction Générale des Impôts',
  [TypeExport.CRPI_TRESOR]: 'CRPI — Direction du Trésor',
  [TypeExport.SITUATION_BCEAO]: 'Situation Statistique BCEAO',
  [TypeExport.DOSSIERS_EN_RETARD]: 'Dossiers en retard',
  [TypeExport.ACTIVITE_MENSUELLE]: "Rapport d'activité mensuelle",
  [TypeExport.FICHE_DOSSIER]: 'Fiche dossier',
  [TypeExport.HISTORIQUE_DOSSIER]: 'Historique du dossier'
};