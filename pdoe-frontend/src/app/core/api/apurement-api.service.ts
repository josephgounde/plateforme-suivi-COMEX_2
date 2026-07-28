// API Apurement (PDOE_openapi.yaml, tags Apurement + Paiements). Les alertes
// J-14/J-8/J=0 sont planifiées par le Scheduler nocturne ; ce service ne fait qu'agir sur leurs conséquences.

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Dossier,
  AlerteApurement,
  PaiementPartiel,
  PaiementListResponse,
  CreatePaiementRequest
} from '../models/dossier.model';

// Item individuel de la checklist d'apurement
export interface ChecklistItem {
  libelle: string;
  valide: boolean;
}

@Injectable({ providedIn: 'root' })
export class ApurementApiService {
  private readonly base = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // POST /apurement/{dossierId}/checklist — passe en APURE si tousValides et solde restant nul.
  validerChecklist(
    dossierId: number,
    items: ChecklistItem[],
    tousValides: boolean
  ): Observable<Dossier> {
    return this.http.post<Dossier>(
      `${this.base}/apurement/${dossierId}/checklist`,
      { items, tousValides }
    );
  }

  // POST /apurement/{dossierId}/declarer-depassement
  // Déclaration obligatoire à J=0, enregistre le dépassement et génère l'export CRPI FINEX.
  declarerDepassement(
    dossierId: number,
    dateDeclaration: string,
    montantNonApure: number
  ): Observable<Dossier> {
    return this.http.post<Dossier>(
      `${this.base}/apurement/${dossierId}/declarer-depassement`,
      { dateDeclaration, montantNonApure }
    );
  }

  // GET /apurement/{dossierId}/alertes — historique (RELANCE_J14, MISE_EN_DEMEURE_J8, DEPASSEMENT_J0) + statut d'envoi.
  getAlertes(dossierId: number): Observable<AlerteApurement[]> {
    return this.http.get<AlerteApurement[]>(
      `${this.base}/apurement/${dossierId}/alertes`
    );
  }

  // Paiements partiels — rattachés au dossier mais gérés comme partie du cycle d'apurement.

  // GET /dossiers/{dossierId}/paiements-partiels
  getPaiements(dossierId: number): Observable<PaiementListResponse> {
    return this.http.get<PaiementListResponse>(
      `${this.base}/dossiers/${dossierId}/paiements-partiels`
    );
  }

  // POST /dossiers/{dossierId}/paiements-partiels
  // Contrôle anti-double-paiement côté backend : somme des paiements ≤ montant initial du dossier.
  createPaiement(
    dossierId: number,
    req: CreatePaiementRequest
  ): Observable<PaiementPartiel> {
    return this.http.post<PaiementPartiel>(
      `${this.base}/dossiers/${dossierId}/paiements-partiels`,
      req
    );
  }
}
