// Checklist d'apurement configurable (mock) — anciennement une constante
// figée dans ApurementDetailComponent, désormais administrable.

import { Injectable } from '@angular/core';
import { ChecklistItemConfig } from '../models/dossier.model';

const SEED_DATE = '2025-01-06T08:00:00.000Z';

@Injectable({ providedIn: 'root' })
export class MockChecklistConfigStore {
  // Public et mutable : ce store EST la table, pas une façade en lecture seule.
  items: ChecklistItemConfig[] = [
    { checklistItemId: 1, libelle: 'Justificatif douanier (D3/BAE) reçu et vérifié', ordre: 1, actif: true, createdAt: SEED_DATE, updatedAt: SEED_DATE, updatedBy: 'SYSTEM' },
    { checklistItemId: 2, libelle: "Conformité du montant avec l'opération déclarée", ordre: 2, actif: true, createdAt: SEED_DATE, updatedAt: SEED_DATE, updatedBy: 'SYSTEM' },
    { checklistItemId: 3, libelle: 'Documents archivés électroniquement', ordre: 3, actif: true, createdAt: SEED_DATE, updatedAt: SEED_DATE, updatedBy: 'SYSTEM' }
  ];

  prochainId = 4;

  // Un item désactivé n'est plus proposé aux nouveaux chargements, sans effet rétroactif sur une saisie en cours.
  get itemsActifs(): ChecklistItemConfig[] {
    return this.items.filter(i => i.actif).sort((a, b) => a.ordre - b.ordre);
  }
}
