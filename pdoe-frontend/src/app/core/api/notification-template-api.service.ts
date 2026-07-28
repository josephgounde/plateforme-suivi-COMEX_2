// API modèles de notification (PDOE_openapi.yaml, tag NotificationTemplates).
// Modification réservée à ADMIN_DSIRI/SUPER_ADMIN, contrôlé côté backend.

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { NotificationTemplate, NotificationTemplateUpdateRequest } from '../models/dossier.model';

@Injectable({ providedIn: 'root' })
export class NotificationTemplateApiService {
  private readonly base = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // GET /notification-templates
  list(): Observable<NotificationTemplate[]> {
    return this.http.get<NotificationTemplate[]>(`${this.base}/notification-templates`);
  }

  // PATCH /notification-templates/{typeEvenement}
  modifier(typeEvenement: string, req: NotificationTemplateUpdateRequest): Observable<NotificationTemplate> {
    return this.http.patch<NotificationTemplate>(
      `${this.base}/notification-templates/${typeEvenement}`,
      req
    );
  }
}
