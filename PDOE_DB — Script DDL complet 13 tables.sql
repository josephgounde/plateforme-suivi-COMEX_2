-- ============================================================
--  PDOE_DB — Script DDL complet pour 13 tables
--  SGBD : Microsoft SQL Server (on-premise AFBCI)
--  Auteur : DSIRI — Équipe développement PDOE
--  Date   : Juillet 2026
-- ------------------------------------------------------------
--  Modifications :
--  + Table Utilisateurs (NOUVELLE) : Gestion locale des profils (CRUD Admin)
--  + Table GestionnaireClients (NOUVELLE) : Portefeuilles automatiques
--  + Table Dossiers :
--    - Ajout GestionnaireAssigneLogin (Affectation manuelle Admin)
--  + Ajout des Clés Étrangères (FK) restrictives vers Utilisateurs(LoginAD)
--  + Mise à jour des vues et insertion des données de graine (Seed)
--  + Table WorkflowEtapes (NOUVELLE) : Workflow modulaire — étapes
--    réordonnables/désactivables/personnalisables sans déploiement
--  + Table JournalAudit (NOUVELLE, v5.5) : journal d'audit — connexions,
--    actions admin et circuit des dossiers — réservé Super Admin (Admin DSIRI exclu)
--  + Table NotificationTemplates (NOUVELLE, v5.6) : modèles de
--    notification (libellé, message, canal) configurables par
--    typeEvenement — Admin DSIRI ou Super Admin
--  + Table ChecklistItemsConfig (NOUVELLE, v5.6) : items de la
--    checklist d'apurement configurables (SEQ-04) — même principe que
--    WorkflowEtapes, Admin DSIRI ou Super Admin
--  + v5.7 : suivi du dossier physique papier retiré (StatutPhysique,
--    Dossiers/EtapesWorkflow.DossierPhysiqueTransmis/Recu et leurs dates,
--    VW_DossiersCours.StatutPhysique) — la PDOE ne trace plus que le
--    circuit électronique.
--  + v5.9 : Dossiers.PaysDest renommé PaysBeneficiaire ; ajout
--    MatriculeClient/NomBeneficiaire/NatureTransaction/TypeCompteDebite
--    (NOT NULL) et CodeSwiftIndicatif/BanqueCorrespondanteIndicative
--    (NULL, indicatifs Agent d'accueil) — alignement sur le formulaire
--    de demande de transfert réel côté frontend (dossier.model.ts).
--  + v5.10 : Dossiers.EmailClient/TelephoneClient (notifier-client) ;
--    Dossiers.EtapeGeneriqueCode/SousEtatGenerique — moteur de circuit
--    dynamique piloté par WorkflowEtapes.Ordre/Actif (Valider/Rejeter/
--    Soumettre n'utilisent plus de table de transition figée).
--  + v5.11 : PaiementsPartiels — ajout UQ_PaiementsPartiels_Reference
--    (DossierId, ReferencePaiement), documentée dans PDOE_openapi.yaml
--    depuis le départ mais jamais posée en base.
--  + v5.12 : Table ExportsReglementaires (NOUVELLE) : trace structurée de
--    chaque export réglementaire généré (CRPI DGI/Trésor, Situation
--    BCEAO) — type, période couverte, copie archivée (chemin + hash
--    SHA-256 + taille), qui/quand. Complète JournalAudit (catégorie
--    REPORTING, nouvelle) qui référence chaque ligne via EntiteId, sans
--    y dupliquer le chemin du fichier.
--  + v5.13 : Dossiers.ReferenceCOMEX renommé ReferenceInterne
--    (PDOE-{yyyyMM}-{seq}) — la vraie référence COMEX, au sens du
--    processus physique, est attribuée à la déclaration d'exécution
--    (Dossiers.ReferenceSWIFT), pas à la création du dossier.
--  + v5.14 : Dossiers.ReferenceDomiciliation/CodeStatistiqueOperateur/
--    NifClient (NULL, saisis à l'Initiation) et Documents.
--    ReferenceDocument (NULL, n° de référence du document lui-même —
--    facture, AC, formulaire de change — distinct de NomFichier) :
--    champs réglementaires identifiés absents lors du contrôle du
--    dossier/formulaire face à la checklist de domiciliation BCEAO
--    (import de marchandises / prestations de services).
--  + v5.15 : Dossiers.NumeroAC/CodeTRF (NULL) — n° d'Attestation/
--    Autorisation de Change et code TRF (formulaire de change),
--    déplacés de Documents.ReferenceDocument vers Dossiers : saisis par
--    l'Agent COMEX à la déclaration d'exécution (ReferenceABS/
--    ReferenceSWIFT), pas à l'upload du document par l'Agent d'accueil.
--  + v5.16 : ParametrageMetier — restauration de 6 clés retrouvées dans
--    une sauvegarde antérieure du projet (absentes de la base actuelle,
--    sans trace de suppression via l'audit PARAMETRAGE ni via l'API —
--    jamais réellement recréées depuis) : SEUIL_FDI_MONTANT (seuil
--    réel, avec logique métier — cf. SoumettreDossierHandler bloquant
--    la soumission d'un IMPORT_BIENS sans document FDI joint au-delà du
--    seuil) ; SEUIL_ALERTE_PCT_1/2, SEUIL_ESCALADE_PCT et
--    NOTIFICATION_RETRY_MAX/DELAI_MIN (paramètres réintroduits tels
--    quels, aucune logique métier ne les consomme encore — prévu avec
--    la refonte du module Notifications).
--  + v5.17 : Dossiers.AdressePostaleClient/AdresseGeographiqueClient/
--    CodeBanque/QualiteResidence/DateOuvertureCompte/AnneeExerciceCompte
--    (NULL, saisis à l'Initiation dans la section "Compte client") et
--    TelephoneClient (colonne déjà existante depuis v5.10 mais jamais
--    alimentable à la création — désormais dans CreateDossierRequest/
--    UpdateDossierRequest).
--  + v5.18 : ParametrageMetier — les 6 délais réglementaires d'apurement
--    (DELAI_APUREMENT_*/DELAI_PAIEMENT_EXPORT_BIENS_J) passent
--    ModifiableUI 0→1, avec bornes ValeurMin/Max '1'/'365' (jours) pour
--    empêcher une valeur absurde. Modifiable par Admin DSIRI/Super Admin
--    depuis l'écran Paramétrage — même mécanisme que SEUIL_FDI_MONTANT.
--  + v5.19 : ExportsReglementaires.Categorie (NOUVELLE, NOT NULL,
--    REGLEMENTAIRE|OPERATIONNEL) — la table (nom historique conservé)
--    trace désormais TOUS les exports Reporting, pas seulement CRPI/
--    BCEAO : DOSSIERS_EN_RETARD et ACTIVITE_MENSUELLE rejoignent
--    TypeExport, archivés/hashés/journalisés selon le même mécanisme.
--  + v5.20 : NotificationTemplates — RELANCE_J14/MISE_EN_DEMEURE_J8/
--    DEPASSEMENT_J0 (NOUVEAUX). AlertesApurement plaçait déjà ces 3
--    TypeAlerte en base (DeclarerExecutionHandler) mais aucun modèle
--    ne leur correspondait — NotificationWriter restait silencieux
--    faute de template. Complète AlerteApurementSchedulerService
--    (nouveau job PDOE.Workflow.API/BackgroundJobs) qui déclenche ces
--    3 alertes à échéance et NotificationRetryService (PDOE.Notifications/
--    BackgroundJobs) qui referme enfin le retry NbTentatives/ECHEC.
-- ============================================================

USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'PDOE_DB')
BEGIN
    PRINT 'La base PDOE_DB existe déjà. Script interrompu pour éviter un écrasement.';
END
ELSE
BEGIN
    CREATE DATABASE PDOE_DB
        COLLATE French_CI_AS;
    PRINT 'Base PDOE_DB créée avec succès.';
END
GO

USE PDOE_DB;
GO

-- ============================================================
--  0. NETTOYAGE DES TABLES EXISTANTES (Ordre des dépendances)
-- ============================================================
IF OBJECT_ID('dbo.JournalAudit', 'U') IS NOT NULL DROP TABLE dbo.JournalAudit;
IF OBJECT_ID('dbo.NotificationTemplates', 'U') IS NOT NULL DROP TABLE dbo.NotificationTemplates;
IF OBJECT_ID('dbo.ChecklistItemsConfig', 'U') IS NOT NULL DROP TABLE dbo.ChecklistItemsConfig;
IF OBJECT_ID('dbo.Notifications', 'U') IS NOT NULL DROP TABLE dbo.Notifications;
IF OBJECT_ID('dbo.AlertesApurement', 'U') IS NOT NULL DROP TABLE dbo.AlertesApurement;
IF OBJECT_ID('dbo.Documents', 'U') IS NOT NULL DROP TABLE dbo.Documents;
IF OBJECT_ID('dbo.PaiementsPartiels', 'U') IS NOT NULL DROP TABLE dbo.PaiementsPartiels;
IF OBJECT_ID('dbo.EtapesWorkflow', 'U') IS NOT NULL DROP TABLE dbo.EtapesWorkflow;
IF OBJECT_ID('dbo.WorkflowEtapes', 'U') IS NOT NULL DROP TABLE dbo.WorkflowEtapes;
IF OBJECT_ID('dbo.GestionnaireClients', 'U') IS NOT NULL DROP TABLE dbo.GestionnaireClients;
IF OBJECT_ID('dbo.Dossiers', 'U') IS NOT NULL DROP TABLE dbo.Dossiers;
IF OBJECT_ID('dbo.Utilisateurs', 'U') IS NOT NULL DROP TABLE dbo.Utilisateurs;
IF OBJECT_ID('dbo.ParametrageMetier', 'U') IS NOT NULL DROP TABLE dbo.ParametrageMetier;
GO

-- ============================================================
--  1. TABLE : ParametrageMetier
-- ============================================================
CREATE TABLE dbo.ParametrageMetier
(
    ParametreId     INT             NOT NULL    IDENTITY(1,1),
    Cle             NVARCHAR(100)   NOT NULL,
    Valeur          NVARCHAR(200)   NOT NULL,
    Unite           NVARCHAR(30)    NOT NULL,
    Description     NVARCHAR(500)   NOT NULL,
    ModifiableUI    BIT             NOT NULL    CONSTRAINT DF_Param_ModifiableUI DEFAULT (1),
    ValeurMin       NVARCHAR(50)    NULL,
    ValeurMax       NVARCHAR(50)    NULL,
    CreatedAt       DATETIME2       NOT NULL    CONSTRAINT DF_Param_CreatedAt DEFAULT (GETUTCDATE()),
    UpdatedAt       DATETIME2       NOT NULL    CONSTRAINT DF_Param_UpdatedAt DEFAULT (GETUTCDATE()),
    CreatedBy       NVARCHAR(100)   NOT NULL,
    UpdatedBy       NVARCHAR(100)   NOT NULL,

    CONSTRAINT PK_ParametrageMetier PRIMARY KEY CLUSTERED (ParametreId ASC)
);
GO
CREATE UNIQUE NONCLUSTERED INDEX IX_ParametrageMetier_Cle ON dbo.ParametrageMetier (Cle);
GO

-- ============================================================
--  2. TABLE : Utilisateurs 
-- ============================================================
CREATE TABLE dbo.Utilisateurs
(
    UtilisateurId   INT             NOT NULL    IDENTITY(1,1),
    LoginAD         NVARCHAR(100)   NOT NULL, -- Clé pivot extraite du JWT
    Nom             NVARCHAR(100)   NOT NULL,
    Prenom          NVARCHAR(100)   NOT NULL,
    Email           NVARCHAR(150)   NOT NULL,
    Profil          NVARCHAR(30)    NOT NULL,
    EstActif        BIT             NOT NULL    CONSTRAINT DF_Utilisateurs_EstActif DEFAULT (1),
    
    CreatedAt       DATETIME2       NOT NULL    CONSTRAINT DF_Utilisateurs_CreatedAt DEFAULT (GETUTCDATE()),
    UpdatedAt       DATETIME2       NOT NULL    CONSTRAINT DF_Utilisateurs_UpdatedAt DEFAULT (GETUTCDATE()),
    CreatedBy       NVARCHAR(100)   NOT NULL,
    UpdatedBy       NVARCHAR(100)   NOT NULL,

    CONSTRAINT PK_Utilisateurs PRIMARY KEY CLUSTERED (UtilisateurId ASC),
    CONSTRAINT UQ_Utilisateurs_LoginAD UNIQUE (LoginAD),
    CONSTRAINT CK_Utilisateurs_Profil
        CHECK (Profil IN ('AGENT_ACCUEIL', 'GESTIONNAIRE', 'AGENT_COMEX', 'TRESORERIE', 'DIRECTION', 'ADMIN_DSIRI', 'SUPER_ADMIN'))
);
GO

-- ============================================================
--  3. TABLE : Dossiers 
-- ============================================================
CREATE TABLE dbo.Dossiers
(
    DossierId                       INT             NOT NULL    IDENTITY(1,1),
    -- N° interne (PDOE-{yyyyMM}-{seq}), attribué à la création — distinct
    -- de ReferenceSWIFT ci-dessous, la vraie référence COMEX du processus
    -- physique, attribuée plus tard à la déclaration d'exécution.
    ReferenceInterne                NVARCHAR(30)    NOT NULL,
    NumCompte                       NVARCHAR(20)    NOT NULL,
    NomClient                       NVARCHAR(150)   NOT NULL,
    TypeOperation                   NVARCHAR(20)    NOT NULL,
    Montant                         DECIMAL(18,4)   NOT NULL    CONSTRAINT CK_Dossiers_Montant CHECK (Montant > 0),
    Devise                          NCHAR(3)        NOT NULL,
    PaysBeneficiaire                NVARCHAR(100)   NOT NULL,
    Motif                           NVARCHAR(500)   NOT NULL,

    -- Initiation (v5.9) — saisis par l'Agent d'accueil à la création,
    -- alignés sur le formulaire réel côté frontend (dossier.model.ts)
    MatriculeClient                 NVARCHAR(50)    NOT NULL,
    NomBeneficiaire                 NVARCHAR(150)   NOT NULL,
    NatureTransaction               NVARCHAR(200)   NOT NULL,
    -- Champs réglementaires (v5.14) — checklist de domiciliation BCEAO,
    -- saisis à l'Initiation quand connus.
    ReferenceDomiciliation           NVARCHAR(50)    NULL,
    CodeStatistiqueOperateur         NVARCHAR(50)    NULL,
    NifClient                        NVARCHAR(50)    NULL,
    -- Informations client complémentaires (v5.17) — section "Compte
    -- client" du formulaire d'Initiation.
    AdressePostaleClient             NVARCHAR(250)   NULL,
    AdresseGeographiqueClient        NVARCHAR(250)   NULL,
    CodeBanque                       NVARCHAR(10)    NULL,
    QualiteResidence                 NVARCHAR(20)    NULL    CONSTRAINT CK_Dossiers_QualiteResidence CHECK (QualiteResidence IS NULL OR QualiteResidence IN ('RESIDENT', 'NON_RESIDENT')),
    DateOuvertureCompte              DATE            NULL,
    AnneeExerciceCompte              INT             NULL,
    TypeCompteDebite                NVARCHAR(20)    NOT NULL,
    -- Indicatifs préliminaires (Agent d'accueil) — distincts de
    -- CorrespondantDesigne/BicCorrespondant ci-dessous, confirmés par la
    -- Trésorerie à l'étape 4.
    CodeSwiftIndicatif              NVARCHAR(11)    NULL,
    BanqueCorrespondanteIndicative  NVARCHAR(200)   NULL,

    StatutElectronique              NVARCHAR(50)    NOT NULL,

    -- Étape générique courante (v5.10) — renseigné uniquement quand le
    -- dossier est positionné sur une étape WorkflowEtapes personnalisée
    -- (TypeEtape = GENERIQUE, sans dashboard dédié) ; StatutElectronique
    -- reste alors figé à sa dernière valeur historique jusqu'au retour
    -- sur une étape du circuit historique.
    EtapeGeneriqueCode               NVARCHAR(30)    NULL,
    SousEtatGenerique                NVARCHAR(20)    NULL,

    -- Habilitations & Routages (v5.3)
    -- Si renseigné par l'Admin, ce gestionnaire prime sur le portefeuille automatique
    GestionnaireAssigneLogin        NVARCHAR(100)   NULL, 

    -- Contrôles Signatures (v5.2)
    SignatureValideeABS             BIT             NOT NULL    CONSTRAINT DF_Dossiers_SignatureValideeABS DEFAULT (0),
    DateValidationSignature         DATETIME2       NULL,
    ModeVerificationApplique        NVARCHAR(20)    NOT NULL    CONSTRAINT DF_Dossiers_ModeVerif DEFAULT ('AUTOMATIQUE'),
    SignatureVerifieeVisuellement   BIT             NOT NULL    CONSTRAINT DF_Dossiers_SigVisuelle DEFAULT (0),
    InitialesAgent                  NVARCHAR(10)    NULL,

    -- Workflows Étape par Étape
    DateConfirmationClient          DATETIME2       NULL,
    SoldeCompteVerifie              BIT             NOT NULL    CONSTRAINT DF_Dossiers_SoldeCompteVerifie DEFAULT (0),

    -- Coordonnées client (v5.9) — pour notifier-client (SMS/Email) ;
    -- pas encore de lookup CBS pour ces champs, NULL tant qu'aucune
    -- source ne les alimente.
    EmailClient                     NVARCHAR(150)   NULL,
    TelephoneClient                 NVARCHAR(30)    NULL,
    TauxChange                      DECIMAL(18,6)   NULL,
    DeviseCotation                  NCHAR(3)        NULL,
    CorrespondantDesigne            NVARCHAR(200)   NULL,
    BicCorrespondant                NCHAR(11)       NULL,
    DateDebit                       DATE            NULL,
    Couverture                      NVARCHAR(200)   NULL,
    DisponibiliteFonds              BIT             NOT NULL    CONSTRAINT DF_Dossiers_DisponibiliteFonds DEFAULT (0),

    ReferenceABS                    NVARCHAR(50)    NULL,
    ReferenceSWIFT                  NVARCHAR(50)    NULL,
    NumeroAC                        NVARCHAR(30)    NULL,
    CodeTRF                         NVARCHAR(30)    NULL,
    DateExecution                   DATETIME2       NULL,
    MontantExecute                  DECIMAL(18,4)   NULL        CONSTRAINT CK_Dossiers_MontantExecute CHECK (MontantExecute IS NULL OR MontantExecute > 0),

    DateEcheanceApurement           DATE            NULL,
    SoldeRestantApurement           DECIMAL(18,4)   NULL,
    ApurementComplet                BIT             NOT NULL    CONSTRAINT DF_Dossiers_ApurementComplet DEFAULT (0),

    CreatedAt                       DATETIME2       NOT NULL    CONSTRAINT DF_Dossiers_CreatedAt DEFAULT (GETUTCDATE()),
    UpdatedAt                       DATETIME2       NOT NULL    CONSTRAINT DF_Dossiers_UpdatedAt DEFAULT (GETUTCDATE()),
    CreatedBy                       NVARCHAR(100)   NOT NULL,
    UpdatedBy                       NVARCHAR(100)   NOT NULL,

    CONSTRAINT PK_Dossiers PRIMARY KEY CLUSTERED (DossierId ASC),
    
    -- Clés Étrangères Habilitations
    CONSTRAINT FK_Dossiers_GestionnaireAssigne
        FOREIGN KEY (GestionnaireAssigneLogin) REFERENCES dbo.Utilisateurs (LoginAD)
        ON DELETE NO ACTION ON UPDATE NO ACTION,

    CONSTRAINT CK_Dossiers_ModeVerification CHECK (ModeVerificationApplique IN ('AUTOMATIQUE', 'VISUEL', 'LES_DEUX')),
    CONSTRAINT CK_Dossiers_TypeOperation CHECK (TypeOperation IN ('IMPORT_BIENS', 'IMPORT_SERVICES', 'EXPORT_BIENS', 'EXPORT_SERVICES', 'TRANSFERT_CAPITAUX')),
    CONSTRAINT CK_Dossiers_TypeCompteDebite CHECK (TypeCompteDebite IN ('COURANT', 'EPARGNE', 'DEVISE')),
    CONSTRAINT CK_Dossiers_SignatureDateCoherence CHECK (SignatureValideeABS = 0 OR DateValidationSignature IS NOT NULL),
    CONSTRAINT CK_Dossiers_VerifVisuelleCoherence CHECK (SignatureVerifieeVisuellement = 0 OR InitialesAgent IS NOT NULL),
    CONSTRAINT CK_Dossiers_ExecutionApurementCoherence CHECK (DateExecution IS NULL OR DateEcheanceApurement IS NOT NULL),
    CONSTRAINT CK_Dossiers_SousEtatGenerique CHECK (SousEtatGenerique IS NULL OR SousEtatGenerique IN ('EN_ATTENTE', 'VALIDE', 'REJETE')),
    CONSTRAINT CK_Dossiers_EtapeGeneriqueCoherence CHECK (
        (EtapeGeneriqueCode IS NULL AND SousEtatGenerique IS NULL) OR
        (EtapeGeneriqueCode IS NOT NULL AND SousEtatGenerique IS NOT NULL)
    )
);
GO
CREATE UNIQUE NONCLUSTERED INDEX IX_Dossiers_ReferenceInterne ON dbo.Dossiers (ReferenceInterne);
GO
CREATE NONCLUSTERED INDEX IX_Dossiers_NumCompte ON dbo.Dossiers (NumCompte);
GO
CREATE NONCLUSTERED INDEX IX_Dossiers_StatutElectronique ON dbo.Dossiers (StatutElectronique) INCLUDE (DossierId, ReferenceInterne, NomClient, UpdatedAt);
GO

-- ============================================================
--  4. TABLE : GestionnaireClients 
-- ============================================================
CREATE TABLE dbo.GestionnaireClients
(
    GestionnaireClientId    INT             NOT NULL    IDENTITY(1,1),
    GestionnaireLogin       NVARCHAR(100)   NOT NULL,
    NumCompte               NVARCHAR(20)    NOT NULL,
    DateAffectation         DATETIME2       NOT NULL    CONSTRAINT DF_GC_DateAffectation DEFAULT (GETUTCDATE()),
    
    CreatedAt               DATETIME2       NOT NULL    CONSTRAINT DF_GC_CreatedAt DEFAULT (GETUTCDATE()),
    CreatedBy               NVARCHAR(100)   NOT NULL,

    CONSTRAINT PK_GestionnaireClients PRIMARY KEY CLUSTERED (GestionnaireClientId ASC),
    CONSTRAINT FK_GestionnaireClients_Utilisateur
        FOREIGN KEY (GestionnaireLogin) REFERENCES dbo.Utilisateurs (LoginAD)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT UQ_GestionnaireClients_Portefeuille UNIQUE (GestionnaireLogin, NumCompte)
);
GO
CREATE NONCLUSTERED INDEX IX_GestionnaireClients_Lookup ON dbo.GestionnaireClients (GestionnaireLogin) INCLUDE (NumCompte);
GO

-- ============================================================
--  5. TABLE : EtapesWorkflow
-- ============================================================
CREATE TABLE dbo.EtapesWorkflow
(
    EtapeId                     INT             NOT NULL    IDENTITY(1,1),
    DossierId                   INT             NOT NULL,
    NiveauValidation            NVARCHAR(30)    NOT NULL,
    StatutAvant                 NVARCHAR(50)    NOT NULL,
    StatutApres                 NVARCHAR(50)    NOT NULL,
    Action                      NVARCHAR(30)    NOT NULL,
    MotifRejet                  NVARCHAR(1000)  NULL,
    ResponsableCorrection       NVARCHAR(200)   NULL,
    AgentLogin                  NVARCHAR(100)   NOT NULL,
    DateAction                  DATETIME2       NOT NULL,
    CreatedAt                   DATETIME2       NOT NULL    CONSTRAINT DF_EtapesWF_CreatedAt DEFAULT (GETUTCDATE()),
    CreatedBy                   NVARCHAR(100)   NOT NULL,

    CONSTRAINT PK_EtapesWorkflow PRIMARY KEY CLUSTERED (EtapeId ASC),
    CONSTRAINT FK_EtapesWorkflow_Dossiers FOREIGN KEY (DossierId) REFERENCES dbo.Dossiers (DossierId),
    CONSTRAINT FK_EtapesWorkflow_Agent FOREIGN KEY (AgentLogin) REFERENCES dbo.Utilisateurs (LoginAD),
    CONSTRAINT CK_EtapesWF_MotifRejet CHECK (Action <> 'REJET' OR MotifRejet IS NOT NULL),
    CONSTRAINT CK_EtapesWF_ResponsableCorrection CHECK (Action <> 'REJET' OR ResponsableCorrection IS NOT NULL)

    -- NiveauValidation référence dbo.WorkflowEtapes.Code par convention applicative,
    -- PAS par FK : cette table est un historique immuable (INSERT ONLY) qui doit
    -- rester lisible même si une étape personnalisée est renommée ou retirée de la
    -- configuration active plus tard. Même convention que StatutAvant/StatutApres
    -- (NVARCHAR libre, validé côté API, pas de table de référence StatutDossier).
);
GO

-- ============================================================
--  5-bis. TABLE : WorkflowEtapes
--  Étapes configurables du circuit — remplace les 7 valeurs figées
--  de l'ancien enum NiveauValidation par une table pilotable par
--  l'Admin DSIRI (réordonnancement, activation/désactivation, ajout
--  d'étapes personnalisées) sans déploiement de code. Ordre pilote le
--  routage réel des dossiers (cf. moteur de transition côté API).
-- ============================================================
CREATE TABLE dbo.WorkflowEtapes
(
    EtapeConfigId   INT             NOT NULL    IDENTITY(1,1),
    Code            NVARCHAR(30)    NOT NULL,   -- ex 'ETAPE_3_COMEX' (historique) ou 'ETAPE_8_CONFORMITE' (custom)
    Libelle         NVARCHAR(100)   NOT NULL,
    Ordre           INT             NOT NULL,   -- position 1..n, pilote le routage réel
    Actif           BIT             NOT NULL    CONSTRAINT DF_WFEtapes_Actif DEFAULT (1),
    TypeEtape       NVARCHAR(20)    NOT NULL,   -- écran cible : voir CK ci-dessous
    Description     NVARCHAR(500)   NULL,

    CreatedAt       DATETIME2       NOT NULL    CONSTRAINT DF_WFEtapes_CreatedAt DEFAULT (GETUTCDATE()),
    UpdatedAt       DATETIME2       NOT NULL    CONSTRAINT DF_WFEtapes_UpdatedAt DEFAULT (GETUTCDATE()),
    CreatedBy       NVARCHAR(100)   NOT NULL,
    UpdatedBy       NVARCHAR(100)   NOT NULL,

    CONSTRAINT PK_WorkflowEtapes PRIMARY KEY CLUSTERED (EtapeConfigId ASC),
    CONSTRAINT UQ_WorkflowEtapes_Code UNIQUE (Code),
    CONSTRAINT UQ_WorkflowEtapes_Ordre UNIQUE (Ordre),
    CONSTRAINT CK_WorkflowEtapes_TypeEtape
        CHECK (TypeEtape IN ('GESTIONNAIRE', 'COMEX', 'TRESORERIE', 'EXECUTION', 'APUREMENT', 'GENERIQUE')),
    CONSTRAINT CK_WorkflowEtapes_Ordre CHECK (Ordre > 0)
);
GO
CREATE NONCLUSTERED INDEX IX_WorkflowEtapes_Actif_Ordre ON dbo.WorkflowEtapes (Actif, Ordre);
GO

-- ============================================================
--  5-ter. TABLE : ChecklistItemsConfig (v5.6)
--  Items de la checklist d'apurement (écran Apurement, SEQ-04, bloc
--  "Checklist d'apurement") — auparavant une liste figée côté frontend,
--  désormais pilotable par l'Admin DSIRI (ajout/désactivation/
--  réordonnancement) sans déploiement de code. Même principe que
--  dbo.WorkflowEtapes ci-dessus : pas de suppression physique, un item
--  se désactive (Actif = 0), jamais ne se supprime.
-- ============================================================
CREATE TABLE dbo.ChecklistItemsConfig
(
    ChecklistItemId INT             NOT NULL    IDENTITY(1,1),
    Libelle         NVARCHAR(300)   NOT NULL,
    Ordre           INT             NOT NULL,   -- position 1..n, pilote l'ordre d'affichage
    Actif           BIT             NOT NULL    CONSTRAINT DF_ChecklistItems_Actif DEFAULT (1),

    CreatedAt       DATETIME2       NOT NULL    CONSTRAINT DF_ChecklistItems_CreatedAt DEFAULT (GETUTCDATE()),
    UpdatedAt       DATETIME2       NOT NULL    CONSTRAINT DF_ChecklistItems_UpdatedAt DEFAULT (GETUTCDATE()),
    CreatedBy       NVARCHAR(100)   NOT NULL,
    UpdatedBy       NVARCHAR(100)   NOT NULL,

    CONSTRAINT PK_ChecklistItemsConfig PRIMARY KEY CLUSTERED (ChecklistItemId ASC),
    CONSTRAINT UQ_ChecklistItemsConfig_Ordre UNIQUE (Ordre),
    CONSTRAINT CK_ChecklistItemsConfig_Ordre CHECK (Ordre > 0)
);
GO
CREATE NONCLUSTERED INDEX IX_ChecklistItemsConfig_Actif_Ordre ON dbo.ChecklistItemsConfig (Actif, Ordre);
GO

-- ============================================================
--  6. TABLES SECONDAIRES (Paiements, Documents, Alertes, Notifs)
-- ============================================================
CREATE TABLE dbo.PaiementsPartiels
(
    PaiementId          INT             NOT NULL    IDENTITY(1,1),
    DossierId           INT             NOT NULL,
    MontantPaiement     DECIMAL(18,4)   NOT NULL    CONSTRAINT CK_Paiements_Montant CHECK (MontantPaiement > 0),
    Devise              NCHAR(3)        NOT NULL,
    DatePaiement        DATE            NOT NULL,
    ReferencePaiement   NVARCHAR(100)   NOT NULL,
    SoldeRestant        DECIMAL(18,4)   NOT NULL    CONSTRAINT CK_Paiements_SoldeRestant CHECK (SoldeRestant >= 0),
    CreatedAt           DATETIME2       NOT NULL    CONSTRAINT DF_Paiements_CreatedAt DEFAULT (GETUTCDATE()),
    CreatedBy           NVARCHAR(100)   NOT NULL,

    CONSTRAINT PK_PaiementsPartiels PRIMARY KEY CLUSTERED (PaiementId ASC),
    CONSTRAINT FK_PaiementsPartiels_Dossiers FOREIGN KEY (DossierId) REFERENCES dbo.Dossiers (DossierId),
    CONSTRAINT UQ_PaiementsPartiels_Reference UNIQUE (DossierId, ReferencePaiement)
);
GO

CREATE TABLE dbo.Documents
(
    DocumentId      INT             NOT NULL    IDENTITY(1,1),
    DossierId       INT             NOT NULL,
    PaiementId      INT             NULL,
    TypeDocument    NVARCHAR(30)    NOT NULL,
    -- N° de référence du document lui-même (v5.14) — ex : n° de facture,
    -- n° d'AC, n° de formulaire de change — distinct de NomFichier.
    ReferenceDocument NVARCHAR(100) NULL,
    NomFichier      NVARCHAR(255)   NOT NULL,
    CheminIIS       NVARCHAR(500)   NOT NULL,
    HashSHA256      NCHAR(64)       NOT NULL,
    TailleFichier   BIGINT          NOT NULL    CONSTRAINT CK_Documents_Taille CHECK (TailleFichier > 0),
    EstObligatoire  BIT             NOT NULL    CONSTRAINT DF_Documents_EstObligatoire DEFAULT (0),
    EstValide       BIT             NOT NULL    CONSTRAINT DF_Documents_EstValide DEFAULT (0),
    CreatedAt       DATETIME2       NOT NULL    CONSTRAINT DF_Documents_CreatedAt DEFAULT (GETUTCDATE()),
    CreatedBy       NVARCHAR(100)   NOT NULL,

    CONSTRAINT PK_Documents PRIMARY KEY CLUSTERED (DocumentId ASC),
    CONSTRAINT FK_Documents_Dossiers FOREIGN KEY (DossierId) REFERENCES dbo.Dossiers (DossierId),
    CONSTRAINT FK_Documents_PaiementsPartiels FOREIGN KEY (PaiementId) REFERENCES dbo.PaiementsPartiels (PaiementId) ON DELETE SET NULL
);
GO

CREATE TABLE dbo.AlertesApurement
(
    AlerteId    INT             NOT NULL    IDENTITY(1,1),
    DossierId   INT             NOT NULL,
    TypeAlerte  NVARCHAR(30)    NOT NULL,
    JRestants   INT             NOT NULL    CONSTRAINT CK_Alertes_JRestants CHECK (JRestants >= 0),
    DateAlerte  DATETIME2       NOT NULL,
    Envoye      BIT             NOT NULL    CONSTRAINT DF_Alertes_Envoye DEFAULT (0),
    DateEnvoi   DATETIME2       NULL,
    CreatedAt   DATETIME2       NOT NULL    CONSTRAINT DF_Alertes_CreatedAt DEFAULT (GETUTCDATE()),
    CreatedBy   NVARCHAR(100)   NOT NULL,

    CONSTRAINT PK_AlertesApurement PRIMARY KEY CLUSTERED (AlerteId ASC),
    CONSTRAINT FK_AlertesApurement_Dossiers FOREIGN KEY (DossierId) REFERENCES dbo.Dossiers (DossierId),
    CONSTRAINT UQ_AlertesApurement_Unicite UNIQUE (DossierId, TypeAlerte)
);
GO

CREATE TABLE dbo.Notifications
(
    NotificationId      INT             NOT NULL    IDENTITY(1,1),
    DossierId           INT             NULL,
    TypeEvenement       NVARCHAR(100)   NOT NULL,
    Canal               NVARCHAR(20)    NOT NULL,
    Destinataire        NVARCHAR(200)   NOT NULL,
    Sujet               NVARCHAR(300)   NULL,
    Corps               NVARCHAR(MAX)   NOT NULL,
    MessageIdGateway    NVARCHAR(100)   NULL,
    Statut              NVARCHAR(20)    NOT NULL    CONSTRAINT DF_Notifs_Statut DEFAULT ('EN_ATTENTE'),
    CodeErreur          NVARCHAR(50)    NULL,
    NbTentatives        INT             NOT NULL    CONSTRAINT DF_Notifs_NbTentatives DEFAULT (0),
    DateEnvoi           DATETIME2       NULL,
    CreatedAt           DATETIME2       NOT NULL    CONSTRAINT DF_Notifs_CreatedAt DEFAULT (GETUTCDATE()),
    CreatedBy           NVARCHAR(100)   NOT NULL,

    CONSTRAINT PK_Notifications PRIMARY KEY CLUSTERED (NotificationId ASC),
    CONSTRAINT FK_Notifications_Dossiers FOREIGN KEY (DossierId) REFERENCES dbo.Dossiers (DossierId) ON DELETE SET NULL
);
GO

-- Modèles de notification (v5.6) — un par TypeEvenement (ex:
-- DOSSIER_SOUMIS, DOSSIER_REJETE). Libelle alimente la cloche/le
-- panneau de notifications des dashboards, Corps est le gabarit du
-- message SMS/Email envoyé (cf. Notifications.Corps, généré à partir
-- de ce gabarit au moment de l'envoi). Clé primaire = TypeEvenement
-- (pas d'IDENTITY) : l'ensemble des événements possibles est fixé par
-- le code applicatif, une ligne par événement, jamais de doublon ni de
-- suppression — seul le contenu est administrable.
CREATE TABLE dbo.NotificationTemplates
(
    TypeEvenement   NVARCHAR(100)   NOT NULL,
    Libelle         NVARCHAR(150)   NOT NULL,
    Corps           NVARCHAR(1000)  NOT NULL,
    CanalDefaut     NVARCHAR(20)    NOT NULL    CONSTRAINT DF_NotifTemplates_CanalDefaut DEFAULT ('EMAIL'),

    UpdatedAt       DATETIME2       NOT NULL    CONSTRAINT DF_NotifTemplates_UpdatedAt DEFAULT (GETUTCDATE()),
    UpdatedBy       NVARCHAR(100)   NOT NULL,

    CONSTRAINT PK_NotificationTemplates PRIMARY KEY CLUSTERED (TypeEvenement ASC),
    CONSTRAINT CK_NotifTemplates_CanalDefaut CHECK (CanalDefaut IN ('SMS', 'EMAIL', 'SMS_ET_EMAIL'))
);
GO

-- Journal d'audit — vue centralisée cross-dossiers pour le Super Admin :
-- authentification, actions admin (utilisateurs, paramétrage, config du
-- circuit) et désormais chaque transition de dossier (soumission,
-- validation, rejet, exécution, apurement...). EtapesWorkflow reste
-- l'historique détaillé d'un dossier donné (onglet Historique) ; ici,
-- même événement, vue globale toutes catégories confondues. Pas de
-- FK vers Utilisateurs : une ligne d'audit doit survivre à la suppression
-- ou à la désactivation du compte qu'elle décrit.
CREATE TABLE dbo.JournalAudit
(
    JournalAuditId  INT             NOT NULL    IDENTITY(1,1),
    Categorie       NVARCHAR(30)    NOT NULL,   -- AUTHENTIFICATION / UTILISATEUR / PARAMETRAGE / WORKFLOW / REPORTING
    TypeAction      NVARCHAR(50)    NOT NULL,   -- CONNEXION_REUSSIE, UTILISATEUR_CREE, SOUMISSION, VALIDATION, REJET, EXPORT_REGLEMENTAIRE, ...
    Description     NVARCHAR(500)   NOT NULL,   -- résumé lisible, généré au moment de l'écriture
    EntiteType      NVARCHAR(50)    NULL,       -- 'Utilisateur' / 'ParametrageMetier' / 'WorkflowEtapes' / 'Dossier' / NULL pour un événement d'authentification
    EntiteId        NVARCHAR(50)    NULL,       -- identifiant générique (chaîne — supporte à la fois des ids numériques et des codes)
    Succes          BIT             NOT NULL    CONSTRAINT DF_JournalAudit_Succes DEFAULT (1),
    DateAction      DATETIME2       NOT NULL    CONSTRAINT DF_JournalAudit_DateAction DEFAULT (GETUTCDATE()),
    CreatedBy       NVARCHAR(100)   NOT NULL,   -- login de l'acteur, ou le login tenté en cas d'échec de connexion

    CONSTRAINT PK_JournalAudit PRIMARY KEY CLUSTERED (JournalAuditId ASC)
);
GO

-- Trace structurée de TOUT export généré depuis Reporting — réglementaire
-- (CRPI DGI/Trésor, Situation BCEAO) et opérationnel (Dossiers en retard,
-- Activité mensuelle) : type, période couverte, copie archivée sur disque
-- (chemin + hash SHA-256 + taille, même principe que Documents), qui/quand.
-- Nom de table historique (réglementaire), conservé pour éviter un
-- renommage — Categorie distingue désormais les deux familles.
-- Pas de FK : indépendante des Dossiers (couvre une période, pas un dossier
-- précis) et doit elle aussi survivre à la désactivation d'un compte.
CREATE TABLE dbo.ExportsReglementaires
(
    ExportReglementaireId  INT             NOT NULL    IDENTITY(1,1),
    Categorie                NVARCHAR(20)    NOT NULL    CONSTRAINT DF_ExportsReglementaires_Categorie DEFAULT ('REGLEMENTAIRE') CONSTRAINT CK_ExportsReglementaires_Categorie CHECK (Categorie IN ('REGLEMENTAIRE', 'OPERATIONNEL')),
    TypeExport              NVARCHAR(30)    NOT NULL,   -- CRPI_DGI / CRPI_TRESOR / SITUATION_BCEAO / DOSSIERS_EN_RETARD / ACTIVITE_MENSUELLE
    DateDebut               DATE            NOT NULL,
    DateFin                 DATE            NOT NULL,
    NomFichier               NVARCHAR(255)   NOT NULL,
    CheminFichier            NVARCHAR(500)   NOT NULL,
    HashSHA256               NCHAR(64)       NOT NULL,
    TailleFichier            BIGINT          NOT NULL    CONSTRAINT CK_ExportsReglementaires_Taille CHECK (TailleFichier > 0),
    CreatedAt                DATETIME2       NOT NULL    CONSTRAINT DF_ExportsReglementaires_CreatedAt DEFAULT (GETUTCDATE()),
    CreatedBy                NVARCHAR(100)   NOT NULL,

    CONSTRAINT PK_ExportsReglementaires PRIMARY KEY CLUSTERED (ExportReglementaireId ASC)
);
GO

-- ============================================================
--  7. VUES UTILITAIRES AJUSTÉES
-- ============================================================
CREATE OR ALTER VIEW dbo.VW_DossiersCours AS
SELECT
    d.DossierId, d.ReferenceInterne, d.NomClient, d.NumCompte,
    d.TypeOperation, d.Montant, d.Devise,
    d.StatutElectronique,
    d.ModeVerificationApplique, d.GestionnaireAssigneLogin,
    d.DateEcheanceApurement, d.SoldeRestantApurement,
    e.NiveauValidation              AS DerniereEtape,
    we.Libelle                      AS DerniereEtapeLibelle,
    we.Ordre                        AS DerniereEtapeOrdre,
    we.Actif                        AS DerniereEtapeActive,
    e.AgentLogin                    AS DernierAgent,
    e.DateAction                    AS DateDerniereAction
FROM dbo.Dossiers d
OUTER APPLY (
    SELECT TOP 1 * FROM dbo.EtapesWorkflow
    WHERE DossierId = d.DossierId ORDER BY DateAction DESC
) e
-- LEFT JOIN (pas INNER) : une étape renommée/retirée après le passage
-- du dossier ne doit jamais faire disparaître le dossier de cette vue.
LEFT JOIN dbo.WorkflowEtapes we ON we.Code = e.NiveauValidation
WHERE d.StatutElectronique NOT IN ('ARCHIVE', 'REJETE_DEFINITIF');
GO

-- ============================================================
--  8. SEED DATA (Paramétrages & Premier Administrateur)
-- ============================================================

-- Insertion du premier compte Admin (pour permettre l'accès initial et le CRUD d'installation)
INSERT INTO dbo.Utilisateurs (LoginAD, Nom, Prenom, Email, Profil, EstActif, CreatedBy, UpdatedBy)
VALUES ('admin.dsiri', 'DSIRI', 'Responsable Platform', 'dsiri.comex@afrilandfirstbank.ci', 'ADMIN_DSIRI', 1, 'SYSTEM', 'SYSTEM');

-- Comptes de test — un par profil restant, alignés sur les logins du
-- mock frontend (mock-utilisateurs.store.ts) pour pouvoir tester les
-- mêmes identifiants des deux côtés.
INSERT INTO dbo.Utilisateurs (LoginAD, Nom, Prenom, Email, Profil, EstActif, CreatedBy, UpdatedBy)
VALUES
    ('agent.accueil', 'Konan', 'Adjoua', 'agent.accueil@afbci.ci', 'AGENT_ACCUEIL', 1, 'SYSTEM', 'SYSTEM'),
    ('gestionnaire.diallo', 'Diallo', 'Fatoumata', 'gestionnaire.diallo@afbci.ci', 'GESTIONNAIRE', 1, 'SYSTEM', 'SYSTEM'),
    ('gestionnaire.kone', 'Koné', 'Yves', 'gestionnaire.kone@afbci.ci', 'GESTIONNAIRE', 1, 'SYSTEM', 'SYSTEM'),
    ('comex', 'Bamba', 'Serge', 'comex@afbci.ci', 'AGENT_COMEX', 1, 'SYSTEM', 'SYSTEM'),
    ('tresorerie', 'Kouassi', 'Michelle', 'tresorerie@afbci.ci', 'TRESORERIE', 1, 'SYSTEM', 'SYSTEM'),
    ('direction', 'N''Guessan', 'Paul', 'direction@afbci.ci', 'DIRECTION', 1, 'SYSTEM', 'SYSTEM'),
    ('super.admin', 'Gounde', 'Joseph', 'super.admin@afbci.ci', 'SUPER_ADMIN', 1, 'SYSTEM', 'SYSTEM');

-- Insertion des Paramètres Métier
-- (Les anciens interrupteurs WF_ETAPE_COMEX_ACTIVEE / WF_SEUIL_MIN_MONTANT_COMEX
-- ont été retirés : superseded par dbo.WorkflowEtapes.Actif, cf. section 5-bis —
-- une colonne BIT sur une vraie table de configuration est strictement préférable
-- à un booléen encodé en chaîne dans un magasin clé-valeur générique.)
INSERT INTO dbo.ParametrageMetier
    (Cle, Valeur, Unite, Description, ModifiableUI, ValeurMin, ValeurMax, CreatedBy, UpdatedBy)
VALUES
    ('MODE_VERIFICATION_SIGNATURE', 'AUTOMATIQUE', 'mode', 'Mode de vérification signature : AUTOMATIQUE | VISUEL | LES_DEUX', 1, NULL, NULL, 'SYSTEM', 'SYSTEM'),
    ('DELAI_GESTIONNAIRE_HEURES', '4', 'heures', 'Délai max — Validation Gestionnaire (étape 2)', 1, '1', '72', 'SYSTEM', 'SYSTEM'),
    ('DELAI_COMEX_HEURES', '24', 'heures', 'Délai max — Contrôle COMEX (étape 3)', 1, '1', '72', 'SYSTEM', 'SYSTEM'),
    ('DELAI_TRESORERIE_HEURES', '24', 'heures', 'Délai max — Avis Trésorerie (étape 4)', 1, '1', '72', 'SYSTEM', 'SYSTEM'),
    -- Délais réglementaires d'apurement (Règlement N° 09/2010/CM/UEMOA) —
    -- un par type d'opération (cf. dbo.Dossiers.CK_Dossiers_TypeOperation).
    -- Modifiables via l'UI depuis v5.18 (Admin DSIRI/Super Admin), bornés
    -- 1-365 jours. EXPORT_BIENS est le seul type à deux clés : l'échéance
    -- de paiement (défaut 120j après expédition, Annexe II Art. 13/16)
    -- puis le rapatriement (défaut 30j après exigibilité, Annexe II Art.
    -- 15/17) — DeclarerExecutionHandler additionne les deux pour obtenir
    -- l'échéance d'apurement finale.
    ('DELAI_APUREMENT_IMPORT_BIENS_J', '30', 'jours', 'Délai réglementaire apurement import de biens — apurement sous 30 jours après dédouanement (Règlement 09/2010/CM/UEMOA, Annexe II Art. 3, 5, 10 & 12)', 1, '1', '365', 'SYSTEM', 'SYSTEM'),
    ('DELAI_APUREMENT_IMPORT_SERVICES_J', '30', 'jours', 'Délai réglementaire apurement import de services — apurement sous 30 jours suivant la réalisation du service ou la réception de la facture définitive (Règlement 09/2010/CM/UEMOA, Art. 4 & Annexe II Art. 10 & 12)', 1, '1', '365', 'SYSTEM', 'SYSTEM'),
    ('DELAI_PAIEMENT_EXPORT_BIENS_J', '120', 'jours', 'Échéance de paiement maximale pour un export de biens, à compter de l''expédition (Règlement 09/2010/CM/UEMOA, Annexe II Art. 13 & 16)', 1, '1', '365', 'SYSTEM', 'SYSTEM'),
    ('DELAI_APUREMENT_EXPORT_BIENS_J', '30', 'jours', 'Délai de rapatriement des fonds pour un export de biens, à compter de l''exigibilité du paiement (Règlement 09/2010/CM/UEMOA, Annexe II Art. 15 & 17)', 1, '1', '365', 'SYSTEM', 'SYSTEM'),
    ('DELAI_APUREMENT_EXPORT_SERVICES_J', '30', 'jours', 'Délai de rapatriement des fonds pour un export de services, à compter de l''exigibilité du paiement (Règlement 09/2010/CM/UEMOA, Annexe II Art. 15)', 1, '1', '365', 'SYSTEM', 'SYSTEM'),
    ('DELAI_APUREMENT_TRANSFERT_CAPITAUX_J', '30', 'jours', 'Délai d''apurement documentaire pour un transfert de revenus & capitaux, à compter de l''exécution (Règlement 09/2010/CM/UEMOA, Art. 6, 7, 10 & Titre IV)', 1, '1', '365', 'SYSTEM', 'SYSTEM'),
    ('SEUIL_DOMICILIATION_FCFA', '10000000', 'FCFA', 'Montant au-delà duquel la domiciliation bancaire est obligatoire pour un import ou export de biens (Règlement 09/2010/CM/UEMOA)', 0, NULL, NULL, 'SYSTEM', 'SYSTEM'),
    -- v5.16 — restaurés depuis une sauvegarde antérieure du projet (cf.
    -- changelog ci-dessus). SEUIL_FDI_MONTANT est le seul consommé par une
    -- vraie logique métier (SoumettreDossierHandler) ; les 5 suivants
    -- sont réintroduits tels quels, sans consommateur pour l'instant.
    ('SEUIL_FDI_MONTANT', '5000000', 'XOF', 'Montant à partir duquel la FDI (Fiche de Déclaration d''Importation) est obligatoire pour un import de biens — en-deçà, facultative. Bloque la soumission du dossier (SoumettreDossierHandler) si dépassé sans document FDI joint.', 1, '0', '500000000', 'SYSTEM', 'SYSTEM'),
    ('SEUIL_ALERTE_PCT_1', '50', '%', 'Premier seuil alerte délai — rappel silencieux', 1, '10', '80', 'SYSTEM', 'SYSTEM'),
    ('SEUIL_ALERTE_PCT_2', '90', '%', 'Deuxième seuil alerte — notification active', 1, '50', '99', 'SYSTEM', 'SYSTEM'),
    ('SEUIL_ESCALADE_PCT', '100', '%', 'Seuil escalade hiérarchique Direction', 1, '90', '100', 'SYSTEM', 'SYSTEM'),
    ('NOTIFICATION_RETRY_MAX', '3', 'tentatives', 'Nombre max de tentatives d''envoi SMS/Email', 1, '1', '10', 'SYSTEM', 'SYSTEM'),
    ('NOTIFICATION_RETRY_DELAI_MIN', '5', 'minutes', 'Délai entre deux tentatives de retry', 1, '1', '60', 'SYSTEM', 'SYSTEM');
GO

-- Insertion des 7 étapes historiques du circuit (v5.2/v5.3), désormais
-- pilotables : Ordre et Actif deviennent modifiables par l'Admin DSIRI
-- sans déploiement. TypeEtape route vers le tableau de bord spécialisé
-- existant ; GENERIQUE route vers l'écran de secours (aucun dashboard
-- dédié pour Initiation ni Archivage aujourd'hui).
INSERT INTO dbo.WorkflowEtapes
    (Code, Libelle, Ordre, Actif, TypeEtape, Description, CreatedBy, UpdatedBy)
VALUES
    ('ETAPE_1_INITIATION',   'Initiation',           1, 1, 'GENERIQUE',    'Création du dossier par l''Agent d''accueil', 'SYSTEM', 'SYSTEM'),
    ('ETAPE_2_GESTIONNAIRE', 'Gestionnaire',         2, 1, 'GESTIONNAIRE', 'Validation du Gestionnaire de compte', 'SYSTEM', 'SYSTEM'),
    ('ETAPE_3_COMEX',        'Contrôle COMEX',       3, 1, 'COMEX',        'Contrôle réglementaire et LCB-FT par le COMEX', 'SYSTEM', 'SYSTEM'),
    ('ETAPE_4_TRESORERIE',   'Trésorerie',           4, 1, 'TRESORERIE',   'Avis Trésorerie — taux de change, correspondant', 'SYSTEM', 'SYSTEM'),
    ('ETAPE_5_EXECUTION',    'Exécution',            5, 1, 'EXECUTION',    'Bascule et déclaration d''exécution SWIFT', 'SYSTEM', 'SYSTEM'),
    ('ETAPE_6_APUREMENT',    'Apurement',            6, 1, 'APUREMENT',    'Suivi des justificatifs et échéance BCEAO', 'SYSTEM', 'SYSTEM'),
    ('ETAPE_7_ARCHIVAGE',    'Archivage',            7, 1, 'GENERIQUE',    'Clôture et archivage du dossier', 'SYSTEM', 'SYSTEM');
GO

-- Insertion des items de checklist d'apurement par défaut (v5.6) —
-- reproduit la liste auparavant figée côté frontend (ApurementDetailComponent).
INSERT INTO dbo.ChecklistItemsConfig
    (Libelle, Ordre, Actif, CreatedBy, UpdatedBy)
VALUES
    ('Justificatif douanier (D3/BAE) reçu et vérifié',      1, 1, 'SYSTEM', 'SYSTEM'),
    ('Conformité du montant avec l''opération déclarée',    2, 1, 'SYSTEM', 'SYSTEM'),
    ('Documents archivés électroniquement',                 3, 1, 'SYSTEM', 'SYSTEM');
GO

-- Insertion des modèles de notification par défaut (v5.6) — un par
-- événement métier déclenchant une notification (cf. dbo.Notifications.TypeEvenement).
INSERT INTO dbo.NotificationTemplates
    (TypeEvenement, Libelle, Corps, CanalDefaut, UpdatedBy)
VALUES
    ('DOSSIER_SOUMIS', 'Nouveau dossier reçu', 'Un nouveau dossier COMEX vous a été transmis et attend votre validation.', 'EMAIL', 'SYSTEM'),
    ('DOSSIER_REJETE', 'Dossier rejeté',       'Un dossier que vous avez soumis a été rejeté et nécessite une correction.', 'EMAIL', 'SYSTEM'),
    ('DOSSIER_FRACTIONNEMENT', 'Alerte fractionnement signalée', 'Le COMEX a signalé un possible fractionnement sur un dossier — décision (levée d''alerte ou rejet définitif) en attente.', 'EMAIL', 'SYSTEM'),
    ('RELANCE_J14', 'Relance apurement — échéance dans 14 jours', 'L''échéance d''apurement de ce dossier approche (J-14) — une relance ou un début de justification est attendu.', 'EMAIL', 'SYSTEM'),
    ('MISE_EN_DEMEURE_J8', 'Mise en demeure — échéance dans 8 jours', 'L''échéance d''apurement de ce dossier est à 8 jours — une action est requise avant le dépassement du délai réglementaire BCEAO.', 'EMAIL', 'SYSTEM'),
    ('DEPASSEMENT_J0', 'Échéance atteinte — déclaration BCEAO requise', 'L''échéance réglementaire d''apurement est atteinte sans justification complète — une déclaration de dépassement BCEAO doit être effectuée.', 'EMAIL', 'SYSTEM');
GO

PRINT '============================================================';
PRINT ' PDOE_DB v5.13 — Dossiers.ReferenceCOMEX renommé ReferenceInterne';
PRINT ' (PDOE-{yyyyMM}-{seq}) ; ReferenceSWIFT reste la vraie référence';
PRINT ' COMEX, attribuée à la déclaration d''exécution.';
PRINT ' 13 tables installées avec intégrité relationnelle stricte.';
PRINT '============================================================';
GO