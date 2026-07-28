// Modèles de notification (mock). Store séparé de MockDataService pour que
// NotificationsService puisse résoudre un libellé sans en dépendre.

import { Injectable } from '@angular/core';
import { NotificationTemplate } from '../models/dossier.model';
import { CanalNotification } from '../models/enums.model';

const SEED_DATE = '2025-01-06T08:00:00.000Z';

@Injectable({ providedIn: 'root' })
export class MockNotificationTemplateStore {
  // Public et mutable : ce store EST la table, pas une façade en lecture seule.
  templates: NotificationTemplate[] = [
    {
      typeEvenement: 'DOSSIER_SOUMIS',
      libelle: 'Nouveau dossier reçu',
      message: 'Un nouveau dossier COMEX vous a été transmis et attend votre validation.',
      canalDefaut: CanalNotification.EMAIL,
      updatedAt: SEED_DATE,
      updatedBy: 'SYSTEM'
    },
    {
      typeEvenement: 'DOSSIER_REJETE',
      libelle: 'Dossier rejeté',
      message: 'Un dossier que vous avez soumis a été rejeté et nécessite une correction.',
      canalDefaut: CanalNotification.EMAIL,
      updatedAt: SEED_DATE,
      updatedBy: 'SYSTEM'
    },
    {
      typeEvenement: 'DOSSIER_FRACTIONNEMENT',
      libelle: 'Alerte fractionnement signalée',
      message: 'Le COMEX a signalé un possible fractionnement sur un dossier — décision (levée d\'alerte ou rejet définitif) en attente.',
      canalDefaut: CanalNotification.EMAIL,
      updatedAt: SEED_DATE,
      updatedBy: 'SYSTEM'
    }
  ];

  findByType(typeEvenement: string): NotificationTemplate | undefined {
    return this.templates.find(t => t.typeEvenement === typeEvenement);
  }
}
