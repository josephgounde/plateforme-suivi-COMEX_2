// Édition réutilise le composant de création (vérification signature ABS2000 à un seul endroit).
// ':id/modifier' doit précéder ':id' dans le tableau, sinon Angular matche ':id' en premier.

import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';

const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/dossier-list/dossier-list.component').then(m => m.DossierListComponent)
  },
  {
    path: 'nouveau',
    loadComponent: () =>
      import('./pages/dossier-create/dossier-create.component').then(
        m => m.DossierCreateComponent
      )
  },
  {
    path: ':id/modifier',
    loadComponent: () =>
      import('./pages/dossier-create/dossier-create.component').then(
        m => m.DossierCreateComponent
      )
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./pages/dossier-detail/dossier-detail.component').then(
        m => m.DossierDetailComponent
      )
  }
];

@NgModule({
  imports: [CommonModule, RouterModule.forChild(routes)]
})
export class DossiersModule {}