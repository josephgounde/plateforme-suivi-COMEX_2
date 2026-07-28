// Une seule route vers DashboardRouterComponent, qui choisit la vue selon
// auth.profil. /dashboard est toujours la même URL, quel que soit le profil.

import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';

const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/dashboard-router/dashboard-router.component').then(
        m => m.DashboardRouterComponent
      )
  }
];

@NgModule({
  imports: [CommonModule, RouterModule.forChild(routes)]
})
export class DashboardModule {}