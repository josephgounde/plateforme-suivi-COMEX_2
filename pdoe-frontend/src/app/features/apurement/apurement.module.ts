// Une seule route paramétrée ':id' vers ApurementDetailComponent.

import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';

const routes: Routes = [
  {
    path: ':id',
    loadComponent: () =>
      import('./pages/apurement-detail/apurement-detail.component').then(
        m => m.ApurementDetailComponent
      )
  }
];

@NgModule({
  imports: [CommonModule, RouterModule.forChild(routes)]
})
export class ApurementModule {}