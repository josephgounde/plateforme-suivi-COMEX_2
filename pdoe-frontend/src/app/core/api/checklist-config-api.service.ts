// API checklist d'apurement (PDOE_openapi.yaml, tag ChecklistConfig).
// Utilisé par l'écran admin "Checklist d'apurement" et ApurementDetailComponent.

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ChecklistItemConfig,
  ChecklistItemConfigCreateRequest,
  ChecklistItemConfigUpdateRequest
} from '../models/dossier.model';

@Injectable({ providedIn: 'root' })
export class ChecklistConfigApiService {
  private readonly base = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // GET /checklist-config/items
  list(): Observable<ChecklistItemConfig[]> {
    return this.http.get<ChecklistItemConfig[]>(`${this.base}/checklist-config/items`);
  }

  // POST /checklist-config/items
  creer(req: ChecklistItemConfigCreateRequest): Observable<ChecklistItemConfig> {
    return this.http.post<ChecklistItemConfig>(`${this.base}/checklist-config/items`, req);
  }

  // PATCH /checklist-config/items/{checklistItemId}
  modifier(checklistItemId: number, req: ChecklistItemConfigUpdateRequest): Observable<ChecklistItemConfig> {
    return this.http.patch<ChecklistItemConfig>(
      `${this.base}/checklist-config/items/${checklistItemId}`,
      req
    );
  }

  // PATCH /checklist-config/items/reordonner
  reordonner(ordre: number[]): Observable<ChecklistItemConfig[]> {
    return this.http.patch<ChecklistItemConfig[]>(`${this.base}/checklist-config/items/reordonner`, { ordre });
  }
}
