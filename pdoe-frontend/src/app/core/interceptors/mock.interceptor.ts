// Intercepteur mock (style fonctionnel Angular 17+) : court-circuite les
// requêtes HTTP vers MockDataService quand environment.useMock = true.

import { inject } from '@angular/core';
import { HttpErrorResponse, HttpInterceptorFn, HttpResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { delay } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { MockDataService, MockHttpError } from '../mock/mock-data.service';

// Plus aucune surface listée ici : /utilisateurs (PDOE.Admin.API) et /auth/logout (PDOE.Gateway) ont maintenant
// un contrôleur backend réel — regex conservée vide pour ne pas casser d'éventuel futur besoin similaire.
const TOUJOURS_MOCKE = /^$/;

export const mockInterceptor: HttpInterceptorFn = (req, next) => {
  const url = req.url.replace(/.*\/api/, '');

  if (!environment.useMock && !TOUJOURS_MOCKE.test(url)) {
    return next(req);
  }

  const mock = inject(MockDataService);
  const latence = 300 + Math.random() * 200;

  // Traduit MockHttpError en HttpErrorResponse pour que les appelants passent par leur callback `error`.
  try {
    const body = mock.handleRequest(req);

    console.log('[MOCK]', req.method, req.url, '→', body);

    if (body !== null) {
      return of(new HttpResponse({ status: 200, body })).pipe(delay(latence));
    }

    return next(req);
  } catch (error) {
    if (error instanceof MockHttpError) {
      console.warn('[MOCK]', req.method, req.url, '→', error.status, error.message);
      return throwError(() => new HttpErrorResponse({
        status: error.status,
        error: { message: error.message },
        url: req.url
      })).pipe(delay(latence));
    }

    throw error;
  }
};
