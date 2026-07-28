// API gestion des utilisateurs (table Utilisateurs). Annuaire local : LDAP
// authentifie le compte, mais le profil PDOE et son activation vivent ici.

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Utilisateur, CreerUtilisateurRequest, ModifierUtilisateurRequest } from '../models/dossier.model';

@Injectable({ providedIn: 'root' })
export class UtilisateurApiService {
  private readonly base = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // GET /utilisateurs
  list(): Observable<Utilisateur[]> {
    return this.http.get<Utilisateur[]>(`${this.base}/utilisateurs`);
  }

  // POST /utilisateurs
  creer(req: CreerUtilisateurRequest): Observable<Utilisateur> {
    return this.http.post<Utilisateur>(`${this.base}/utilisateurs`, req);
  }

  // PATCH /utilisateurs/{id}
  modifier(utilisateurId: number, req: ModifierUtilisateurRequest): Observable<Utilisateur> {
    return this.http.patch<Utilisateur>(`${this.base}/utilisateurs/${utilisateurId}`, req);
  }
}
