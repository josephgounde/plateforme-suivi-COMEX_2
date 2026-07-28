// Guard d'authentification et de rôle : vérifie la session active, puis le
// profil si la route déclare route.data['roles'] (ex. /admin → ADMIN_DSIRI).

import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { ProfilUtilisateur } from '../models/enums.model';

@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {
  constructor(private auth: AuthService, private router: Router) {}

  canActivate(route: ActivatedRouteSnapshot): boolean {
    if (!this.auth.isAuthenticated) {
      this.router.navigate(['/auth/login']);
      return false;
    }

    const rolesRequis: ProfilUtilisateur[] | undefined = route.data['roles'];
    if (rolesRequis?.length && !this.auth.hasRole(...rolesRequis)) {
      // Profil connecté mais non autorisé pour cette route —
      // renvoi vers le tableau de bord plutôt qu'un écran d'erreur.
      this.router.navigate(['/dashboard']);
      return false;
    }

    return true;
  }
}