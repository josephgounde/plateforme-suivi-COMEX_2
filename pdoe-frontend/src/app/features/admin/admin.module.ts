import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from '../../core/guards/auth.guard';
import { ProfilUtilisateur } from '../../core/models/enums.model';

const routes: Routes = [
  { path: '', redirectTo: 'parametrage', pathMatch: 'full' },
  {
    path: 'parametrage',
    loadComponent: () =>
      import('./pages/parametrage/parametrage.component').then(m => m.ParametrageComponent)
  },
  {
    path: 'utilisateurs',
    loadComponent: () =>
      import('./pages/utilisateurs/utilisateurs.component').then(m => m.UtilisateursComponent)
  },
  {
    path: 'workflow-etapes',
    loadComponent: () =>
      import('./pages/workflow-etapes/workflow-etapes.component').then(m => m.WorkflowEtapesComponent)
  },
  {
    path: 'etapes-generiques',
    loadComponent: () =>
      import('./pages/etapes-generiques/etapes-generiques.component').then(m => m.EtapesGeneriquesComponent)
  },
  {
    path: 'logs-notifications',
    loadComponent: () =>
      import('./pages/logs-notifications/logs-notifications.component').then(m => m.LogsNotificationsComponent)
  },
  {
    path: 'notification-templates',
    loadComponent: () =>
      import('./pages/notification-templates/notification-templates.component').then(m => m.NotificationTemplatesComponent)
  },
  {
    path: 'checklist-config',
    loadComponent: () =>
      import('./pages/checklist-config/checklist-config.component').then(m => m.ChecklistConfigComponent)
  },
  {
    // Exclusif Super Admin — referme l'accès à ce sous-écran même pour un ADMIN_DSIRI entrant par URL directe.
    path: 'journal-audit',
    canActivate: [AuthGuard],
    data: { roles: [ProfilUtilisateur.SUPER_ADMIN] },
    loadComponent: () =>
      import('./pages/journal-audit/journal-audit.component').then(m => m.JournalAuditComponent)
  }
];

@NgModule({
  imports: [CommonModule, RouterModule.forChild(routes)]
})
export class AdminModule {}