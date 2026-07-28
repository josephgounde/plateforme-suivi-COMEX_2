// API étapes du circuit (PDOE_openapi.yaml, tag WorkflowConfig).
// Utilisé par l'écran admin de gestion du circuit et WorkflowStepperComponent.

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  EtapeWorkflowConfig,
  EtapeWorkflowConfigCreateRequest,
  EtapeWorkflowConfigUpdateRequest
} from '../models/dossier.model';

@Injectable({ providedIn: 'root' })
export class WorkflowConfigApiService {
  private readonly base = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // GET /workflow-config/etapes
  list(): Observable<EtapeWorkflowConfig[]> {
    return this.http.get<EtapeWorkflowConfig[]>(`${this.base}/workflow-config/etapes`);
  }

  // POST /workflow-config/etapes
  creer(requete: EtapeWorkflowConfigCreateRequest): Observable<EtapeWorkflowConfig> {
    return this.http.post<EtapeWorkflowConfig>(`${this.base}/workflow-config/etapes`, requete);
  }

  // PATCH /workflow-config/etapes/{code}
  modifier(code: string, requete: EtapeWorkflowConfigUpdateRequest): Observable<EtapeWorkflowConfig> {
    return this.http.patch<EtapeWorkflowConfig>(`${this.base}/workflow-config/etapes/${code}`, requete);
  }

  // PATCH /workflow-config/etapes/reordonner
  reordonner(ordre: string[]): Observable<EtapeWorkflowConfig[]> {
    return this.http.patch<EtapeWorkflowConfig[]>(`${this.base}/workflow-config/etapes/reordonner`, { ordre });
  }
}
