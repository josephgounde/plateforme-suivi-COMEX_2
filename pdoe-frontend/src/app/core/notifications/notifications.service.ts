// Notifications de l'utilisateur connecté, partagées entre la cloche de la
// topbar et le panneau des dashboards pour éviter deux listes qui divergent.

import { Injectable, signal } from '@angular/core';
import { DossierApiService } from '../api/dossier-api.service';
import { Notification } from '../models/dossier.model';
import { MockNotificationTemplateStore } from '../mock/mock-notification-template.store';

@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private readonly _notifications = signal<Notification[]>([]);
  readonly notifications = this._notifications.asReadonly();

  constructor(
    private dossierApi: DossierApiService,
    // Injecté directement pour que le libellé reflète immédiatement un
    // modèle renommé par l'Admin DSIRI.
    private templates: MockNotificationTemplateStore
  ) {}

  // Filtré par destinataire côté API/mock. Idempotente (set, pas push) :
  // safe à appeler depuis chaque consommateur (topbar + panneau).
  charger(): void {
    this.dossierApi.getNotifications().subscribe({
      next: notifs => this._notifications.set(notifs)
    });
  }

  ignorer(notificationId: number): void {
    this._notifications.update(list => list.filter(n => n.notificationId !== notificationId));
  }

  libelle(notification: Notification): string {
    return this.templates.findByType(notification.typeEvenement)?.libelle ?? notification.typeEvenement;
  }
}
