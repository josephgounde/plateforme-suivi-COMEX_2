// API Reporting (PDOE_openapi.yaml, tag Reporting) — vue consolidée et goulots d'étranglement Direction/Admin DSIRI.

import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DashboardData, DossierRetard, ExportReglementaireListResponse } from '../models/dossier.model';
import { CategorieExport, TypeExport } from '../models/enums.model';

export type PeriodeReporting = 'SEMAINE' | 'MOIS' | 'TRIMESTRE' | 'ANNEE';

export interface JournalExportsFilters {
  categorie?: CategorieExport;
  typeExport?: TypeExport;
  dateDebut?: string;
  dateFin?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class ReportingApiService {
  private readonly base = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // GET /reporting/dashboard — répartition par statut, taux d'apurement, retards, alertes.
  getDashboard(periode: PeriodeReporting = 'MOIS'): Observable<DashboardData> {
    const params = new HttpParams().set('periode', periode);
    return this.http.get<DashboardData>(`${this.base}/reporting/dashboard`, { params });
  }

  // GET /reporting/dossiers-en-retard — dossiers dépassant les délais configurés dans ParametrageMetier.
  getDossiersEnRetard(): Observable<DossierRetard[]> {
    return this.http.get<DossierRetard[]>(`${this.base}/reporting/dossiers-en-retard`);
  }

  // POST /reporting/export-crpi-dgi
  // Gabarit officiel DGI rempli (transferts émis/reçus) — classeur .xlsx.
  exportCrpiDgi(dateDebut: string, dateFin: string): Observable<Blob> {
    return this.http.post(
      `${this.base}/reporting/export-crpi-dgi`,
      { dateDebut, dateFin },
      { responseType: 'blob' }
    );
  }

  // POST /reporting/export-crpi-tresor — gabarit officiel Trésor (émis/reçu × UEMOA/hors UEMOA), .xlsx.
  exportCrpiTresor(dateDebut: string, dateFin: string): Observable<Blob> {
    return this.http.post(
      `${this.base}/reporting/export-crpi-tresor`,
      { dateDebut, dateFin },
      { responseType: 'blob' }
    );
  }

  // POST /reporting/export-situation-bceao — gabarit officiel BCEAO (même classification que le Trésor), .xlsx.
  exportSituationBceao(dateDebut: string, dateFin: string): Observable<Blob> {
    return this.http.post(
      `${this.base}/reporting/export-situation-bceao`,
      { dateDebut, dateFin },
      { responseType: 'blob' }
    );
  }

  // GET /reporting/dossiers-en-retard/export — mêmes données que getDossiersEnRetard(), en .xlsx.
  exportDossiersEnRetard(): Observable<Blob> {
    return this.http.get(`${this.base}/reporting/dossiers-en-retard/export`, { responseType: 'blob' });
  }

  // GET /reporting/activite-mensuelle/export — volume, délai moyen, taux de rejet. mois: AAAA-MM, courant si omis.
  exportRapportActiviteMensuel(mois?: string): Observable<Blob> {
    let params = new HttpParams();
    if (mois) params = params.set('mois', mois);
    return this.http.get(`${this.base}/reporting/activite-mensuelle/export`, { params, responseType: 'blob' });
  }

  // GET /reporting/exports — journal de tous les exports générés (réglementaire ET opérationnel).
  getJournalExports(filters: JournalExportsFilters): Observable<ExportReglementaireListResponse> {
    let params = new HttpParams();
    Object.entries(filters).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    });
    return this.http.get<ExportReglementaireListResponse>(`${this.base}/reporting/exports`, { params });
  }

  // GET /reporting/exports/{id}/download — relit le fichier archivé sur le disque (intégrité vérifiée côté serveur).
  downloadJournalExport(exportReglementaireId: number): Observable<Blob> {
    return this.http.get(`${this.base}/reporting/exports/${exportReglementaireId}/download`, { responseType: 'blob' });
  }
}