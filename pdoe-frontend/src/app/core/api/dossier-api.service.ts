// API Dossiers + CBS signature/solde (PDOE_openapi.yaml, tags CBS et Dossiers).

import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Dossier,
  DossierDetail,
  DossierListResponse,
  DossierFilters,
  CreateDossierRequest,
  UpdateDossierRequest,
  TresorerieUpdateRequest,
  SignatureVerificationResult,
  SoldeClientResult,
  TauxChangeResult,
  Document as PdoeDocument,
  Notification,
  JournalAuditEntry,
  ReassignerGestionnaireRequest,
  NotifierClientRequest,
  NotifierClientResponse
} from '../models/dossier.model';
import { ModeVerificationSignature, TypeDocument } from '../models/enums.model';

@Injectable({ providedIn: 'root' })
export class DossierApiService {
  private readonly base = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // CBS — vérification signature ABS2000. GET /clients/{numCompte}/verifier-signature
  verifierSignature(
    numCompte: string,
    mode?: ModeVerificationSignature
  ): Observable<SignatureVerificationResult> {
    let params = new HttpParams();
    if (mode) {
      params = params.set('mode', mode);
    }
    return this.http.get<SignatureVerificationResult>(
      `${this.base}/clients/${numCompte}/verifier-signature`,
      { params }
    );
  }

  // POST /clients/{numCompte}/valider-signature-visuelle
  // Appelé après coche conformité visuelle (modes VISUEL/LES_DEUX) + initiales agent.
  validerSignatureVisuelle(
    numCompte: string,
    initialesAgent: string
  ): Observable<{ signatureValidee: boolean }> {
    return this.http.post<{ signatureValidee: boolean }>(
      `${this.base}/clients/${numCompte}/valider-signature-visuelle`,
      { initialesAgent }
    );
  }

  // GET /clients/{numCompte}/solde
  // Utilisé à l'étape 2 (validation gestionnaire) — lecture seule ABS2000.
  getSoldeClient(numCompte: string): Observable<SoldeClientResult> {
    return this.http.get<SoldeClientResult>(
      `${this.base}/clients/${numCompte}/solde`
    );
  }

  // GET /taux-change?devise=XXX&versDevise=YYY — pré-remplit tauxChange/deviseCotation à l'étape 4 (Trésorerie).
  // versDevise optionnel ; chaque changement de devise de cotation redemande un taux (pas de réutilisation).
  obtenirTauxChange(devise: string, versDevise?: string): Observable<TauxChangeResult> {
    let params = new HttpParams().set('devise', devise);
    if (versDevise) {
      params = params.set('versDevise', versDevise);
    }
    return this.http.get<TauxChangeResult>(`${this.base}/taux-change`, { params });
  }

  // Dossiers — CRUD et cycle de vie

  // GET /dossiers
  // Liste filtrée selon le profil de l'agent connecté (côté backend).
  listDossiers(filters?: DossierFilters): Observable<DossierListResponse> {
    let params = new HttpParams();
    if (filters) {
      Object.entries(filters).forEach(([key, value]) => {
        if (value !== undefined && value !== null && value !== '') {
          params = params.set(key, String(value));
        }
      });
    }
    return this.http.get<DossierListResponse>(`${this.base}/dossiers`, { params });
  }

  // GET /dossiers/{dossierId}
  // Retourne le dossier complet : documents, historique, paiements, alertes.
  getDossier(dossierId: number): Observable<DossierDetail> {
    return this.http.get<DossierDetail>(`${this.base}/dossiers/${dossierId}`);
  }

  // GET /dossiers/{dossierId}/fiche
  // Export opérationnel interne (tous profils) — synthèse d'une page.
  exporterFiche(dossierId: number): Observable<Blob> {
    return this.http.get(`${this.base}/dossiers/${dossierId}/fiche`, { responseType: 'blob' });
  }

  // POST /dossiers
  // Création uniquement après validation positive de la signature ABS2000.
  createDossier(req: CreateDossierRequest): Observable<Dossier> {
    return this.http.post<Dossier>(`${this.base}/dossiers`, req);
  }

  // PUT /dossiers/{dossierId}
  // Mise à jour — typiquement après correction suite à un rejet.
  updateDossier(dossierId: number, req: UpdateDossierRequest): Observable<Dossier> {
    return this.http.put<Dossier>(`${this.base}/dossiers/${dossierId}`, req);
  }

  // PATCH /dossiers/{dossierId}/tresorerie
  // Renseigne les paramètres Trésorerie à l'étape 4.
  updateTresorerie(dossierId: number, req: TresorerieUpdateRequest): Observable<Dossier> {
    return this.http.patch<Dossier>(`${this.base}/dossiers/${dossierId}/tresorerie`, req);
  }

  // POST /dossiers/{dossierId}/soumettre
  // BROUILLON → EN_VALIDATION_GESTIONNAIRE, notifie le gestionnaire assigné.
  soumettreDossier(dossierId: number): Observable<Dossier> {
    return this.http.post<Dossier>(`${this.base}/dossiers/${dossierId}/soumettre`, {});
  }

  // PATCH /dossiers/{dossierId}/gestionnaire
  // Admin uniquement, tant que le dossier n'a pas dépassé l'étape Gestionnaire (vérifié côté serveur).
  reassignerGestionnaire(dossierId: number, req: ReassignerGestionnaireRequest): Observable<Dossier> {
    return this.http.patch<Dossier>(`${this.base}/dossiers/${dossierId}/gestionnaire`, req);
  }

  // Notifications — mise ici plutôt que dans un service dédié pour une seule méthode.

  // GET /notifications — filtré par destinataire côté API/mock.
  getNotifications(): Observable<Notification[]> {
    return this.http.get<Notification[]>(`${this.base}/notifications`);
  }

  // POST /dossiers/{dossierId}/notifier-client
  // Envoi direct au client (dossier en attente de correction), distinct de getNotifications() (interne AFB).
  notifierClient(dossierId: number, req: NotifierClientRequest): Observable<NotifierClientResponse> {
    return this.http.post<NotifierClientResponse>(
      `${this.base}/dossiers/${dossierId}/notifier-client`,
      req
    );
  }

  // Journal d'audit — même raisonnement que Notifications ci-dessus.

  // GET /journal-audit — Admin DSIRI uniquement.
  getJournalAudit(): Observable<JournalAuditEntry[]> {
    return this.http.get<JournalAuditEntry[]>(`${this.base}/journal-audit`);
  }

  // Documents — pièces jointes

  // POST /dossiers/{dossierId}/documents — multipart/form-data, Angular HttpClient sérialise le FormData.
  // estObligatoire défaut false : requis non-nullable par le backend, aucun appelant ne le distingue encore.
  uploaderDocument(
    dossierId: number,
    fichier: File,
    typeDocument: TypeDocument,
    estObligatoire = false,
    referenceDocument?: string
  ): Observable<PdoeDocument> {
    const formData = new FormData();
    formData.append('fichier', fichier, fichier.name);
    formData.append('typeDocument', typeDocument);
    formData.append('estObligatoire', String(estObligatoire));
    if (referenceDocument) {
      formData.append('referenceDocument', referenceDocument);
    }

    return this.http.post<PdoeDocument>(
      `${this.base}/dossiers/${dossierId}/documents`,
      formData
    );
  }
}