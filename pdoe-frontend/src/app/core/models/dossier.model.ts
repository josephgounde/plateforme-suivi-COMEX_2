// Modèles de données : Dossier et entités liées (PDOE_openapi.yaml, components/schemas).

import {
  TypeOperation, StatutDossier, TypeCompte, QualiteResidence,
  ModeVerificationSignature,
  ActionWorkflow, TypeDocument, TypeAlerte,
  CanalNotification, StatutNotification,
  ProfilUtilisateur, TypeEtapeWorkflow, SousEtat, CategorieAudit,
  CategorieExport, TypeExport
} from './enums.model';

// ── DOSSIER — entité centrale ──

export interface Dossier {
  dossierId: number;
  referenceInterne: string;
  numCompte: string;
  nomClient: string;
  typeOperation: TypeOperation;
  montant: number;
  devise: string;
  paysBeneficiaire: string;
  motif: string;

  // Initiation — saisis par l'Agent d'accueil à la création
  matriculeClient: string;
  nomBeneficiaire: string;
  natureTransaction: string;
  // Checklist de domiciliation BCEAO (import/prestations), distincts de referenceInterne et matriculeClient.
  referenceDomiciliation?: string;
  codeStatistiqueOperateur?: string;
  nifClient?: string;
  // Informations client complémentaires — section "Compte client" du
  // formulaire d'Initiation (v5.17).
  adressePostaleClient?: string;
  adresseGeographiqueClient?: string;
  codeBanque?: string;
  qualiteResidence?: QualiteResidence;
  dateOuvertureCompte?: string;
  anneeExerciceCompte?: number;
  typeCompteDebite: TypeCompte;
  // Indicatifs saisis par l'Agent d'accueil, distincts des valeurs confirmées par la Trésorerie (étape 4).
  codeSwiftIndicatif?: string;
  banqueCorrespondanteIndicative?: string;

  // Statuts
  statutElectronique: StatutDossier;

  // true si ce brouillon vient d'un rejet vers l'Agent d'accueil; remis à false à la resoumission.
  estRejeteVersAgentAccueil?: boolean;
  dernierMotifRejet?: string;

  // Vérification signature ABS2000
  modeVerificationApplique: ModeVerificationSignature;
  signatureVerifieeVisuellement: boolean;
  initialesAgent?: string;

  // Validation gestionnaire (étape 2)
  dateConfirmationClient?: string;
  soldeCompteVerifie: boolean;
  // Résultat de la dernière vérification — calculé et persisté par le backend (ObtenirSoldeClientHandler),
  // jamais réglable via updateDossier. undefined tant qu'aucune vérification n'a eu lieu.
  soldeSuffisant?: boolean;
  soldeConstate?: number;
  deviseConstatee?: string;
  dateVerificationSolde?: string;

  // Ajoutées pour permettre au Gestionnaire de notifier le client directement (SMS/Email).
  emailClient?: string;
  telephoneClient?: string;

  // Login du Gestionnaire responsable — réattribuable par l'Admin tant que l'étape Gestionnaire n'est pas dépassée.
  gestionnaireAssigne: string;

  // Paramètres Trésorerie (étape 4)
  tauxChange?: number;
  deviseCotation?: string;
  correspondantDesigne?: string;
  bicCorrespondant?: string;
  dateDebit?: string;
  couverture?: string;
  disponibiliteFonds: boolean;

  // Exécution SWIFT (étape 5)
  referenceABS?: string;
  referenceSWIFT?: string;
  numeroAC?: string;
  codeTRF?: string;
  dateExecution?: string;
  montantExecute?: number;

  // Apurement (étape 6)
  dateEcheanceApurement?: string;
  soldeRestantApurement?: number;
  apurementComplet: boolean;

  // Renseigné seulement sur une étape GENERIQUE personnalisée; jamais actif en même temps que statutElectronique.
  etapeGenerique?: {
    etapeCode: string;
    sousEtat: SousEtat;
  };

  // Audit
  updatedAt: string;
  updatedBy: string;

  // Point de départ du calcul de délai (ParametreMetier.DELAI_*_HEURES) — PAS updatedAt, qui bouge aussi sur des actions internes à l'étape.
  dateDerniereAction: string;
}

// Vue détaillée — dossier + ses entités liées (utilisée sur l'écran de détail)
export interface DossierDetail extends Dossier {
  documents: Document[];
  etapesWorkflow: EtapeWorkflow[];
  paiementsPartiels: PaiementPartiel[];
  alertes: AlerteApurement[];
}

// Réponse paginée de la liste des dossiers
export interface DossierListResponse {
  items: Dossier[];
  total: number;
  page: number;
  pageSize: number;
}

// Filtres applicables à la recherche de dossiers
export interface DossierFilters {
  statut?: StatutDossier;
  typeOperation?: TypeOperation;
  numCompte?: string;
  dateDebutCreation?: string;
  dateFinCreation?: string;
  page?: number;
  pageSize?: number;
}

// ── REQUÊTES — création et mise à jour de dossier ──

export interface CreateDossierRequest {
  numCompte: string;
  nomClient: string;
  typeOperation: TypeOperation;
  montant: number;
  devise: string;
  paysBeneficiaire: string;
  motif: string;
  matriculeClient: string;
  nomBeneficiaire: string;
  natureTransaction: string;
  referenceDomiciliation?: string;
  codeStatistiqueOperateur?: string;
  nifClient?: string;
  adressePostaleClient?: string;
  adresseGeographiqueClient?: string;
  telephoneClient?: string;
  codeBanque?: string;
  qualiteResidence?: QualiteResidence;
  dateOuvertureCompte?: string;
  anneeExerciceCompte?: number;
  typeCompteDebite: TypeCompte;
  codeSwiftIndicatif?: string;
  banqueCorrespondanteIndicative?: string;
  signatureValideeABS: boolean;
  dateValidationSignature: string;
  modeVerificationApplique: ModeVerificationSignature;
  signatureVerifieeVisuellement?: boolean;
  initialesAgent?: string;
}

export interface UpdateDossierRequest {
  typeOperation?: TypeOperation;
  montant?: number;
  devise?: string;
  paysBeneficiaire?: string;
  motif?: string;
  matriculeClient?: string;
  nomBeneficiaire?: string;
  natureTransaction?: string;
  referenceDomiciliation?: string;
  codeStatistiqueOperateur?: string;
  nifClient?: string;
  adressePostaleClient?: string;
  adresseGeographiqueClient?: string;
  telephoneClient?: string;
  codeBanque?: string;
  qualiteResidence?: QualiteResidence;
  dateOuvertureCompte?: string;
  anneeExerciceCompte?: number;
  typeCompteDebite?: TypeCompte;
  codeSwiftIndicatif?: string;
  banqueCorrespondanteIndicative?: string;
  dateConfirmationClient?: string;
}

// Réattribution — n'a de sens que tant que le dossier n'a pas dépassé l'étape Gestionnaire.
export interface ReassignerGestionnaireRequest {
  gestionnaireLogin: string;
}

export interface TresorerieUpdateRequest {
  tauxChange?: number;
  deviseCotation?: string;
  correspondantDesigne?: string;
  bicCorrespondant?: string;
  dateDebit?: string;
  couverture?: string;
  disponibiliteFonds?: boolean;
}

// Notification directe du client (SMS/Email), distincte des notifications internes AFB (cf. Notification ci-dessous).
export interface NotifierClientRequest {
  canal: CanalNotification;
  message: string;
}

export interface NotifierClientResponse {
  succes: boolean;
  destinataire: string;
}

// ── WORKFLOW — historique et transitions ──

export interface EtapeWorkflow {
  etapeId: number;
  // string plutôt que NiveauValidation : doit pouvoir enregistrer un code custom (étape personnalisée), pas seulement les 7 historiques.
  niveauValidation: string;
  statutAvant: StatutDossier;
  statutApres: StatutDossier;
  action: ActionWorkflow;
  motifRejet?: string;
  responsableCorrection?: string;
  agentLogin: string;
  dateAction: string;
}

export interface ValiderEtapeRequest {
  niveauValidation: string;
  dateConfirmationClient?: string;
  soldeCompteVerifie?: boolean;
  conformiteBCEAO?: boolean;
  lcbftConforme?: boolean;
}

export interface RejeterEtapeRequest {
  niveauValidation: string;
  motifRejet: string;

  // Étape de reprise choisie explicitement par qui rejette (pas un login) — doit être une étape déjà franchie et ACTIVE, sinon refusé.
  responsableCorrection: string;
}

export interface WorkflowTransitionResponse {
  dossierId: number;
  referenceInterne: string;
  statutAvant: StatutDossier;
  statutApres: StatutDossier;
  action: ActionWorkflow;
  dateAction: string;
}

// ── DOCUMENT — pièces justificatives ──

export interface Document {
  documentId: number;
  typeDocument: TypeDocument;
  // N° de référence du document (facture, AC, formulaire de change...), distinct de nomFichier.
  referenceDocument?: string;
  nomFichier: string;
  hashSHA256: string;
  tailleFichier: number;
  estObligatoire: boolean;
  estValide: boolean;
  createdAt: string;
  createdBy: string;
}

// ── PAIEMENT PARTIEL — apurement progressif ──

export interface PaiementPartiel {
  paiementId: number;
  montantPaiement: number;
  devise: string;
  datePaiement: string;
  referencePaiement: string;
  soldeRestant: number;
  createdAt: string;
}

export interface PaiementListResponse {
  items: PaiementPartiel[];
  montantInitial: number;
  totalPaye: number;
  soldeRestant: number;
  nbPaiements: number;
}

export interface CreatePaiementRequest {
  montantPaiement: number;
  devise: string;
  datePaiement: string;
  referencePaiement: string;
}

// ── ALERTE APUREMENT — relances réglementaires ──

export interface AlerteApurement {
  alerteId: number;
  typeAlerte: TypeAlerte;
  jRestants: number;
  dateAlerte: string;
  envoye: boolean;
  dateEnvoi?: string;
}

// ── EXÉCUTION SWIFT ──

export interface DeclarerExecutionRequest {
  referenceABS: string;
  referenceSWIFT: string;
  numeroAC?: string;
  codeTRF?: string;
  dateExecution: string;
  montantExecute: number;
}

export interface ExecutionDeclarationResponse {
  dossierId: number;
  referenceInterne: string;
  statutElectronique: StatutDossier;
  referenceABS: string;
  referenceSWIFT: string;
  numeroAC?: string;
  codeTRF?: string;
  dateExecution: string;
  dateEcheanceApurement: string;
  alertesJ14: string;
  alertesJ8: string;
}

// ── CBS — vérification signature ABS2000 ──

export interface SignatureVerificationResult {
  trouve: boolean;
  signatureExistante: boolean;
  nomClient: string;
  typeCompte?: string;
  modeVerification: ModeVerificationSignature;
  // Sert à pré-remplir la section "Compte client" de l'Initiation. codeBanque volontairement absent (pas une donnée ABS2000).
  nifClient?: string;
  adressePostaleClient?: string;
  adresseGeographiqueClient?: string;
  telephoneClient?: string;
  qualiteResidence?: QualiteResidence;
  dateOuvertureCompte?: string;
  anneeExerciceCompte?: number;
}

export interface SoldeClientResult {
  numCompte: string;
  soldeDisponible: number;
  devise: string;
  suffisant: boolean;
  dateConsultation: string;
}

// ── TAUX DE CHANGE — cotation marché (Trésorerie) ──

export interface TauxChangeResult {
  devise: string;
  taux: number;
  deviseCotation: string;
  dateCotation: string;
}

// ── NOTIFICATION ──

export interface Notification {
  notificationId: number;
  dossierId?: number;
  typeEvenement: string;
  canal: CanalNotification;
  destinataire: string;
  statut: StatutNotification;
  codeErreur?: string;
  nbTentatives: number;
  dateEnvoi?: string;
  createdAt: string;
}

// Un modèle par typeEvenement; libelle alimente la cloche/topbar, message est le corps SMS/Email. Configurable par l'Admin sans déploiement.
export interface NotificationTemplate {
  typeEvenement: string;
  libelle: string;
  message: string;
  canalDefaut: CanalNotification;
  updatedAt: string;
  updatedBy: string;
}

export interface NotificationTemplateUpdateRequest {
  libelle?: string;
  message?: string;
  canalDefaut?: CanalNotification;
}

// ── JOURNAL D'AUDIT — actions admin/sécurité, pas le cycle de vie des
// dossiers (déjà couvert par EtapeWorkflow ci-dessus) ──

export interface JournalAuditEntry {
  journalAuditId: number;
  categorie: CategorieAudit;
  typeAction: string;
  description: string;
  entiteType?: string;
  entiteId?: string;
  succes: boolean;
  dateAction: string;
  createdBy: string;
}

// ── PARAMÉTRAGE MÉTIER — dashboard Admin DSIRI ──

export interface ParametreMetier {
  parametreId: number;
  cle: string;
  valeur: string;
  unite: string;
  description: string;
  modifiableUI: boolean;
  valeurMin?: string;
  valeurMax?: string;
  updatedAt: string;
  updatedBy: string;
}

// ── GESTION DES UTILISATEURS — annuaire local (LDAP authentifie, cet
// annuaire mappe le profil PDOE et gère l'activation) ──

export interface Utilisateur {
  utilisateurId: number;
  loginAD: string;
  nom: string;
  prenom: string;
  email: string;
  profil: ProfilUtilisateur;
  estActif: boolean;
  createdAt: string;
  updatedAt: string;
  updatedBy: string;
}

export interface CreerUtilisateurRequest {
  loginAD: string;
  nom: string;
  prenom: string;
  email: string;
  profil: ProfilUtilisateur;
}

export interface ModifierUtilisateurRequest {
  nom?: string;
  prenom?: string;
  email?: string;
  profil?: ProfilUtilisateur;
  estActif?: boolean;
}

// ── REPORTING — dashboards et indicateurs ──

export interface DashboardData {
  totalDossiers: number;
  parStatut: Record<string, number>;
  dossiersEnRetard: number;
  dossiersApurementProche: number;
  tauxApurement: number;
  alertesNonTraitees: number;
}

export interface DossierRetard {
  dossierId: number;
  referenceInterne: string;
  nomClient: string;
  statutElectronique: StatutDossier;
  derniereEtape: string;
  dernierAgent: string;
  heuresDepuisDerniereAction: number;
  seuilDepasse: number;
  // Codes réellement présents dans l'historique — évite une requête séparée par ligne (N+1) pour la liste "dossiers en retard".
  etapesTraverseesCodes: string[];
}

// ── JOURNAL DES EXPORTS (GET /reporting/exports) — pas de cheminFichier : jamais exposé au client ──

export interface ExportReglementaire {
  exportReglementaireId: number;
  categorie: CategorieExport;
  typeExport: TypeExport;
  dateDebut: string;
  dateFin: string;
  nomFichier: string;
  hashSHA256: string;
  tailleFichier: number;
  createdAt: string;
  createdBy: string;
}

export interface ExportReglementaireListResponse {
  items: ExportReglementaire[];
  total: number;
  page: number;
  pageSize: number;
}

// ── WORKFLOW-CONFIG — étapes du circuit configurables (Admin DSIRI) ──

export interface EtapeWorkflowConfig {
  etapeConfigId: number;
  code: string;              // code historique (NiveauValidation) OU personnalisé
  libelle: string;
  ordre: number;
  actif: boolean;
  typeEtape: TypeEtapeWorkflow;
  description?: string;
  createdAt: string;
  updatedAt: string;
  updatedBy: string;
}

export interface EtapeWorkflowConfigCreateRequest {
  code: string;
  libelle: string;
  ordre: number;
  typeEtape?: TypeEtapeWorkflow;
  description?: string;
}

export interface EtapeWorkflowConfigUpdateRequest {
  libelle?: string;
  actif?: boolean;
  description?: string;
}

// ── CHECKLIST D'APUREMENT CONFIGURABLE — anciennement codée en dur,
// désormais administrable sans déploiement (Admin DSIRI) ──

export interface ChecklistItemConfig {
  checklistItemId: number;
  libelle: string;
  ordre: number;
  actif: boolean;
  createdAt: string;
  updatedAt: string;
  updatedBy: string;
}

export interface ChecklistItemConfigCreateRequest {
  libelle: string;
}

export interface ChecklistItemConfigUpdateRequest {
  libelle?: string;
  actif?: boolean;
}

// ── ERREUR API — format standard des erreurs métier ──

export interface ApiError {
  code: string;
  message: string;
  details?: Record<string, unknown>;
}

// ── UTILISATEUR CONNECTÉ — session authentifiée ──

export interface UtilisateurConnecte {
  login: string;
  nomComplet: string;
  email: string;
  profil: string;
  token: string;
  expiresAt: string;
}

// ── VÉRIFICATION OTP — entre le bind LDAP et la délivrance de la session ──

export interface OtpChallenge {
  otpToken: string;
  canal: CanalNotification;
  destinataireMasque: string;
  expiresInSeconds: number;
}

export interface VerifierOtpRequest {
  otpToken: string;
  code: string;
}