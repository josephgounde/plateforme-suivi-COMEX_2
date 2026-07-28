// Aiguilleur : lit auth.profil et rend le bon dashboard enfant, sans logique métier propre.

import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../../core/auth/auth.service';
import { ProfilUtilisateur } from '../../../../core/models/enums.model';
import { AgentAccueilDashboardComponent } from '../agent-accueil-dashboard/agent-accueil-dashboard.component';
import { GestionnaireDashboardComponent } from '../gestionnaire-dashboard/gestionnaire-dashboard.component';
import { ComexDashboardComponent } from '../comex-dashboard/comex-dashboard.component';
import { TresorerieDashboardComponent } from '../tresorerie-dashboard/tresorerie-dashboard.component';
import { DirectionDashboardComponent } from '../direction-dashboard/direction-dashboard.component';
import { AdminDashboardComponent } from '../admin-dashboard/admin-dashboard.component';

@Component({
  selector: 'app-dashboard-router',
  standalone: true,
  imports: [
    CommonModule,
    AgentAccueilDashboardComponent,
    GestionnaireDashboardComponent,
    ComexDashboardComponent,
    TresorerieDashboardComponent,
    DirectionDashboardComponent,
    AdminDashboardComponent
  ],
  template: `
    <app-agent-accueil-dashboard *ngIf="profil === Profil.AGENT_ACCUEIL" />
    <app-gestionnaire-dashboard  *ngIf="profil === Profil.GESTIONNAIRE" />
    <app-comex-dashboard         *ngIf="profil === Profil.AGENT_COMEX" />
    <app-tresorerie-dashboard    *ngIf="profil === Profil.TRESORERIE" />
    <app-direction-dashboard     *ngIf="profil === Profil.DIRECTION" />
    <app-admin-dashboard         *ngIf="profil === Profil.ADMIN_DSIRI || profil === Profil.SUPER_ADMIN" />
  `
})
export class DashboardRouterComponent {
  readonly Profil = ProfilUtilisateur;

  constructor(private auth: AuthService) {}

  get profil(): ProfilUtilisateur {
    return this.auth.profil as ProfilUtilisateur;
  }
}