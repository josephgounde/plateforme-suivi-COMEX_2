// Deux routes sans canActivate (login + otp), la partie de l'app accessible
// sans session. Composants standalone plutôt que sous-modules dédiés.

import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';

const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./pages/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'otp',
    loadComponent: () =>
      import('./pages/otp/otp.component').then(m => m.OtpComponent)
  },
  { path: '', redirectTo: 'login', pathMatch: 'full' }
];

@NgModule({
  imports: [CommonModule, RouterModule.forChild(routes)]
})
export class AuthModule {}