// Panneau de notifications affiché directement sur le dashboard (même flux
// que la cloche de la topbar), pour les profils qui doivent agir dessus vite.

import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { NotificationsService } from '../../../core/notifications/notifications.service';
import { Dossier, Notification } from '../../../core/models/dossier.model';

@Component({
  selector: 'app-notification-panel',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './notification-panel.component.html',
  styleUrl: './notification-panel.component.scss'
})
export class NotificationPanelComponent {
  // Facultatif : permet d'afficher référence/client au lieu du seul dossierId.
  @Input() dossiers: Dossier[] = [];

  constructor(public service: NotificationsService) {}

  // Panneau dashboard = alerte du jour, pas l'historique complet (celui-ci reste
  // consultable via la page Admin > Logs notifications, qui appelle la même API sans ce filtre).
  notificationsDuJour(): Notification[] {
    const aujourdhui = new Date().toDateString();
    return this.service.notifications().filter(
      n => new Date(n.dateEnvoi ?? n.createdAt).toDateString() === aujourdhui
    );
  }

  dossierDe(notification: Notification): Dossier | undefined {
    return this.dossiers.find(d => d.dossierId === notification.dossierId);
  }

  ignorer(notificationId: number): void {
    this.service.ignorer(notificationId);
  }
}
