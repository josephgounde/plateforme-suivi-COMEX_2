// API Paramétrage métier (PDOE_openapi.yaml, tag Parametrage). Seuls les
// paramètres avec modifiableUI = true sont modifiables (le reste, ex. délais BCEAO, est verrouillé côté backend).

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ParametreMetier } from '../models/dossier.model';

@Injectable({ providedIn: 'root' })
export class ParametrageApiService {
  private readonly base = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // GET /parametrage — liste complète, utilisée par l'écran de paramétrage Admin DSIRI.
  list(): Observable<ParametreMetier[]> {
    return this.http.get<ParametreMetier[]>(`${this.base}/parametrage`);
  }

  // GET /parametrage/{cle} — paramètre individuel (ex: MODE_VERIFICATION_SIGNATURE, DELAI_GESTIONNAIRE_HEURES).
  get(cle: string): Observable<ParametreMetier> {
    return this.http.get<ParametreMetier>(`${this.base}/parametrage/${cle}`);
  }

  // PATCH /parametrage/{cle} — le backend valide la plage (valeurMin/valeurMax) et refuse si modifiableUI = false.
  update(cle: string, valeur: string): Observable<ParametreMetier> {
    return this.http.patch<ParametreMetier>(
      `${this.base}/parametrage/${cle}`,
      { valeur }
    );
  }
}