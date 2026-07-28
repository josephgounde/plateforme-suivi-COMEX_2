// API Exécution SWIFT (PDOE_openapi.yaml, tag Execution). Bascule manuelle
// en V1 : l'agent COMEX traite l'opération dans ABS2000/SWIFT puis revient déclarer ici.

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Dossier,
  DeclarerExecutionRequest,
  ExecutionDeclarationResponse
} from '../models/dossier.model';

@Injectable({ providedIn: 'root' })
export class ExecutionApiService {
  private readonly base = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // POST /execution/{dossierId}/basculer — marque EN_EXECUTION_SWIFT, simple changement de statut.
  basculer(dossierId: number): Observable<Dossier> {
    return this.http.post<Dossier>(`${this.base}/execution/${dossierId}/basculer`, {});
  }

  // POST /execution/{dossierId}/declarer — enregistre réf ABS/SWIFT, date, montant exécuté.
  // Déclenche le calcul de l'échéance d'apurement et la planification des alertes J-14/J-8/J=0.
  declarer(
    dossierId: number,
    req: DeclarerExecutionRequest
  ): Observable<ExecutionDeclarationResponse> {
    return this.http.post<ExecutionDeclarationResponse>(
      `${this.base}/execution/${dossierId}/declarer`,
      req
    );
  }

  // GET /execution/{dossierId} — fiche complète : paramètres Trésorerie + données SWIFT post-déclaration.
  getDetail(dossierId: number): Observable<Dossier> {
    return this.http.get<Dossier>(`${this.base}/execution/${dossierId}`);
  }
}