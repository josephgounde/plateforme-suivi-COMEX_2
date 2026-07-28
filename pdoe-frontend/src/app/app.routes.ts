// Routing racine : chaque feature en lazy loading. AuthGuard s'applique à
// toutes les routes sauf /auth ; /admin restreint en plus via data.roles.

import { Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';
import { ProfilUtilisateur } from './core/models/enums.model';
import { AppShellComponent } from './core/layout/app-shell.component';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },

  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.module').then(m => m.AuthModule)
    // Pas de guard ici — c'est justement la route d'entrée
    // pour les utilisateurs non authentifiés.
  },

  // Guard posé sur chaque route enfant, pas sur la coquille : AuthGuard lit route.data['roles'] sur la route qui matche.
  {
    path: '',
    component: AppShellComponent,
    children: [
      {
        path: 'dashboard',
        loadChildren: () =>
          import('./features/dashboard/dashboard.module').then(m => m.DashboardModule),
        canActivate: [AuthGuard]
      },
      {
        path: 'dossiers',
        loadChildren: () =>
          import('./features/dossiers/dossiers.module').then(m => m.DossiersModule),
        canActivate: [AuthGuard]
      },
      {
        path: 'apurement',
        loadChildren: () =>
          import('./features/apurement/apurement.module').then(m => m.ApurementModule),
        canActivate: [AuthGuard]
      },
      {
        path: 'reporting',
        loadChildren: () =>
          import('./features/reporting/reporting.module').then(m => m.ReportingModule),
        canActivate: [AuthGuard],
        // Exclusif Super Admin pour l'instant — à rouvrir à Direction/Admin DSIRI plus tard si besoin confirmé.
        data: { roles: [ProfilUtilisateur.SUPER_ADMIN] }
      },
      {
        path: 'admin',
        loadChildren: () =>
          import('./features/admin/admin.module').then(m => m.AdminModule),
        canActivate: [AuthGuard],
        // SUPER_ADMIN est un superset d'ADMIN_DSIRI; Journal d'audit reste exclusif via son propre data.roles dans admin.module.ts.
        data: { roles: [ProfilUtilisateur.ADMIN_DSIRI, ProfilUtilisateur.SUPER_ADMIN] }
      }
    ]
  },

  { path: '**', redirectTo: 'dashboard' }
];
