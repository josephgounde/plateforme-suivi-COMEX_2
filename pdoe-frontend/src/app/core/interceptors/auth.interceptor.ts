// Ajoute Authorization: Bearer <token> sur toutes les requêtes sortantes.
// Force la déconnexion sur un 401 plutôt que de laisser une session incohérente.

import { Injectable } from '@angular/core';
import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpErrorResponse,
  HttpRequest
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../auth/auth.service';

// Endpoints [AllowAnonymous] appelés avant toute session : un 401 dessus (OTP_INVALID, credentials
// LDAP invalides) est une erreur de saisie, pas une session expirée — ne doit pas déclencher logout().
const URLS_AUTH_ANONYMES = ['/auth/login', '/auth/otp/verifier', '/auth/otp/renvoyer'];

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private auth: AuthService) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const token = this.auth.token;

    const authReq = token
      ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : req;

    return next.handle(authReq).pipe(
      catchError((error: HttpErrorResponse) => {
        const appelAuthAnonyme = URLS_AUTH_ANONYMES.some(url => req.url.includes(url));
        if (error.status === 401 && !appelAuthAnonyme) {
          this.auth.logout();
        }
        return throwError(() => error);
      })
    );
  }
}