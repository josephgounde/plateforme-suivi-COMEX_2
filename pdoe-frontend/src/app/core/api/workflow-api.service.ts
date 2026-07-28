// API Workflow (PDOE_openapi.yaml, tag Workflow). signaler-fractionnement
// est absent du spec v5.2 — à ajouter côté backend (cf. la méthode).

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  EtapeWorkflow,
  WorkflowTransitionResponse,
  ValiderEtapeRequest,
  RejeterEtapeRequest
} from '../models/dossier.model';

// Réponses des contrôles spécifiques étape 3 (Contrôle COMEX)
export interface ControleReglementaireResult {
  conforme: boolean;
  plafondRespecte: boolean;
  codeRetour?: string;
  observations?: string;
}

export interface ControleLcbftResult {
  lcbftConforme: boolean;
  observations?: string;
}

@Injectable({ providedIn: 'root' })
export class WorkflowApiService {
  private readonly base = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // POST /workflow/{dossierId}/valider — INSERT ONLY dans EtapesWorkflow, avance vers l'étape suivante.
  valider(dossierId: number, req: ValiderEtapeRequest): Observable<WorkflowTransitionResponse> {
    return this.http.post<WorkflowTransitionResponse>(
      `${this.base}/workflow/${dossierId}/valider`,
      req
    );
  }

  // POST /workflow/{dossierId}/rejeter
  // motifRejet et responsableCorrection sont obligatoires.
  rejeter(dossierId: number, req: RejeterEtapeRequest): Observable<WorkflowTransitionResponse> {
    return this.http.post<WorkflowTransitionResponse>(
      `${this.base}/workflow/${dossierId}/rejeter`,
      req
    );
  }

  // POST /workflow/{dossierId}/signaler-fractionnement — détection hors PDOE (ABS2000/SWIFT), ici
  // on saisit juste le signalement ; passe en ANTI_FRACTIONNEMENT_DETECTE et notifie Direction/Admin.
  signalerFractionnement(dossierId: number, motif: string): Observable<WorkflowTransitionResponse> {
    return this.http.post<WorkflowTransitionResponse>(
      `${this.base}/workflow/${dossierId}/signaler-fractionnement`,
      { motif }
    );
  }

  // POST /workflow/{dossierId}/lever-alerte
  // Réservé à Direction/Admin DSIRI — pas au COMEX, juge et partie sur sa propre résolution.
  leverAlerte(dossierId: number): Observable<WorkflowTransitionResponse> {
    return this.http.post<WorkflowTransitionResponse>(
      `${this.base}/workflow/${dossierId}/lever-alerte`,
      {}
    );
  }

  // POST /workflow/{dossierId}/rejeter-definitif — clôture irréversible, Direction/Admin DSIRI uniquement.
  rejeterDefinitif(dossierId: number, motif: string): Observable<WorkflowTransitionResponse> {
    return this.http.post<WorkflowTransitionResponse>(
      `${this.base}/workflow/${dossierId}/rejeter-definitif`,
      { motif }
    );
  }

  // POST /workflow/{dossierId}/archiver — Étape 7, Agent COMEX/Admin DSIRI, refusée hors statut EN_ARCHIVAGE.
  archiver(dossierId: number): Observable<WorkflowTransitionResponse> {
    return this.http.post<WorkflowTransitionResponse>(
      `${this.base}/workflow/${dossierId}/archiver`,
      {}
    );
  }

  // GET /workflow/{dossierId}/historique
  // Retourne l'historique complet et immuable des transitions du dossier.
  getHistorique(dossierId: number): Observable<EtapeWorkflow[]> {
    return this.http.get<EtapeWorkflow[]>(`${this.base}/workflow/${dossierId}/historique`);
  }

  // GET /workflow/{dossierId}/historique/export — mêmes données que getHistorique(), en PDF.
  exporterHistorique(dossierId: number): Observable<Blob> {
    return this.http.get(`${this.base}/workflow/${dossierId}/historique/export`, { responseType: 'blob' });
  }

  // POST /workflow/{dossierId}/controle-reglementaire
  // Étape 3 — vérifie la conformité BCEAO/FINEX et les plafonds.
  controleReglementaire(dossierId: number): Observable<ControleReglementaireResult> {
    return this.http.post<ControleReglementaireResult>(
      `${this.base}/workflow/${dossierId}/controle-reglementaire`,
      {}
    );
  }

  // POST /workflow/{dossierId}/controle-lcbft
  // Étape 3 — vérifie la conformité LCB-FT.
  controleLCBFT(dossierId: number): Observable<ControleLcbftResult> {
    return this.http.post<ControleLcbftResult>(
      `${this.base}/workflow/${dossierId}/controle-lcbft`,
      {}
    );
  }
}