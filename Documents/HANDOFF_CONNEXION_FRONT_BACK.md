# Handoff — Connexion frontend ↔ backend réel

Généré le 2026-07-21, mis à jour le 2026-07-21 après la connexion réelle. État vérifié en lisant les 12 (+ Reporting) contrôleurs backend, les 10 services `*-api.service.ts`, `mock.interceptor.ts` et `PDOE_openapi.yaml`, puis en testant en direct dans le navigateur avec le vrai `pdoe-backend` (SQL Server local `JOSEPHGOUNDE\SQLEXPRESS`, base `PDOE_DB`).

## État actuel — la connexion est faite

`environment.useMock = false` : la majorité des appels HTTP passent réellement par `pdoe-backend` (`http://localhost:5072/api`), plus par `MockDataService`. `environment.useMockAuth = true` reste séparé : l'authentification (login/OTP) reste mockée en attendant `PDOE.Gateway`.

`mock.interceptor.ts` garde un allowlist `TOUJOURS_MOCKE` pour les routes sans contrôleur réel : `/clients/*`, `/utilisateurs*`, `/reporting/export-crpi`, `/auth/logout`. Tout le reste passe par le vrai `HttpClient`.

## 1. Câblé et vérifié en direct (dossiers réels vus dans le navigateur)

| Méthode/Path | Frontend | Backend |
|---|---|---|
| GET/POST /dossiers, GET/PUT /dossiers/{id} | `DossierApiService` | `DossiersController` |
| POST /dossiers/{id}/soumettre | `.soumettreDossier` | `.SoumettreDossier` |
| PATCH /dossiers/{id}/tresorerie, /gestionnaire | `DossierApiService` | `DossiersController` |
| POST /dossiers/{id}/notifier-client | `.notifierClient` | `.NotifierClient` |
| GET /dossiers/{id}/fiche | `.exporterFiche` | `.GenererFicheDossier` |
| GET/POST /dossiers/{id}/paiements-partiels | `ApurementApiService` | `DossiersController` |
| POST /dossiers/{id}/documents | `.uploaderDocument` | `DocumentsController.UploadDocument` (FormData corrigé, cf. §4) |
| GET /journal-audit | `.getJournalAudit` | `JournalAuditController` |
| GET /notifications | `.getNotifications` | `NotificationsController` |
| GET /taux-change | `.obtenirTauxChange` | `TauxChangeController` |
| /workflow/{id}/valider, rejeter, lever-alerte, rejeter-definitif, signaler-fractionnement, archiver, controle-reglementaire, controle-lcbft, historique(+export) | `WorkflowApiService` | `WorkflowController` |
| /execution/{id}, /basculer, /declarer | `ExecutionApiService` | `ExecutionController` |
| /apurement/{id}/checklist, declarer-depassement, alertes | `ApurementApiService` | `ApurementController` |
| /parametrage(/{cle}) | `ParametrageApiService` | `ParametrageController` |
| /workflow-config/etapes(+reordonner) | `WorkflowConfigApiService` | `WorkflowConfigController` |
| /checklist-config/items(+reordonner) | `ChecklistConfigApiService` | `ChecklistConfigController` |
| /notification-templates(/{typeEvenement}) | `NotificationTemplateApiService` | `NotificationTemplatesController` |
| GET /reporting/dashboard, /reporting/dossiers-en-retard(+export), /reporting/activite-mensuelle/export | `ReportingApiService` | `ReportingController` (module ajouté le 2026-07-21, requêtes EF Core réelles) |

## 2. Encore mocké — pas de contrôleur réel

- **Authentification** — `POST /auth/login`, `/auth/otp/verifier`, `/auth/otp/renvoyer`, `/auth/logout`. Aucun `AuthController`, `PDOE.Gateway` toujours un stub vide. Reste piloté par `environment.useMockAuth` (indépendant de `useMock`) — sans danger pour le reste puisqu'aucun endpoint backend ne valide le Bearer token.
- **Clients/CBS** — `verifierSignature`, `validerSignatureVisuelle`, `getSoldeClient` → `/clients/{numCompte}/...`. Toujours aucun `ClientsController`.
- **Utilisateurs** — `UtilisateurApiService.list/creer/modifier` → `/utilisateurs`. Toujours aucun `UtilisateursController`.
- **Reporting — un seul endpoint restant** : `POST /reporting/export-crpi` (CRPI FINEX/BCEAO) → confirmé 404 sur le vrai backend, reste sur l'allowlist mock. Le reste du module Reporting est réel depuis le 2026-07-21.

## 3. Endpoints backend sans appelant frontend

- `DocumentsController.ListDocuments` (`GET /dossiers/{id}/documents`) — inutilisé côté frontend.
- `DocumentsController.UpdateDocumentStatut` (`PATCH /dossiers/{id}/documents/{documentId}`) — inutilisé côté frontend.

## 4. Corrections apportées pour que la connexion fonctionne réellement

- **FormData upload** — `uploaderDocument()` envoie maintenant `estObligatoire` (défaut `false`), requis en non-nullable côté `UploadDocumentHandler`.
- **`app.UsePathBase("/api")`** ajouté dans `PDOE.Api/Program.cs` — les contrôleurs déclarent des routes nues (`[Route("dossiers")]`), le préfixe `/api` n'existait auparavant que via un reverse proxy absent en local.
- **CORS dev** — policy `DevCors` ajoutée (`http://localhost:4200`), absente jusque-là ; sans elle le navigateur bloque tout appel cross-origin vers `:5072`.
- **`appsettings.Development.json`** avait une virgule JSON manquante — empêchait le backend de démarrer.
- **`HttpNotificationSender.cs`** ne respectait pas la signature d'`INotificationSender` et utilisait une méthode `HttpContent` inexistante — bloquait la compilation de tout le solution (code mort, jamais branché dans `Program.cs`, mais compilé quand même).
- **Encodage mojibake en base** — les données seed (`ParametrageMetier.Description`, `WorkflowEtapes.Libelle/Description`, `ChecklistItemsConfig.Libelle`, `NotificationTemplates.Libelle/Corps`, `Utilisateurs.Nom` pour `gestionnaire.kone`) avaient été insérées avec un mauvais codepage client (UTF-8 lu comme Windows-1252). Corrigé par un script `UPDATE` ciblé par clé naturelle, aucune modification de schéma, aucune valeur éditée via l'UI Admin DSIRI touchée.

## Reste à faire

1. **Bloquant pour une vraie authentification** : construire `PDOE.Gateway`/`AuthController` (login LDAP + OTP). Rien d'autre n'empêche de tester en conditions réelles sans ça — le reste du backend n'exige aucun token valide aujourd'hui (aucun `[Authorize]` nulle part).
2. `POST /reporting/export-crpi` — dernier endpoint Reporting sans contrôleur.
3. `ClientsController` (signature/solde CBS) et `UtilisateursController` — aucun des deux n'a de contrôleur backend.
