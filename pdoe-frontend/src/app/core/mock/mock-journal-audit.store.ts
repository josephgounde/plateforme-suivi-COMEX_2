// Journal d'audit (mock), reflète dbo.JournalAudit. Store séparé car alimenté par AuthService et MockDataService.

import { Injectable } from '@angular/core';
import { JournalAuditEntry } from '../models/dossier.model';
import { CategorieAudit } from '../models/enums.model';

@Injectable({ providedIn: 'root' })
export class MockJournalAuditStore {
  private readonly entries: JournalAuditEntry[] = [];
  private prochainId = 1;

  enregistrer(params: {
    categorie: CategorieAudit;
    typeAction: string;
    description: string;
    acteur: string;
    succes?: boolean;
    entiteType?: string;
    entiteId?: string;
  }): void {
    this.entries.push({
      journalAuditId: this.prochainId++,
      categorie: params.categorie,
      typeAction: params.typeAction,
      description: params.description,
      entiteType: params.entiteType,
      entiteId: params.entiteId,
      succes: params.succes ?? true,
      dateAction: new Date().toISOString(),
      createdBy: params.acteur
    });
  }

  // Plus récent en premier (dernières actions en haut, pour l'Admin).
  lister(): JournalAuditEntry[] {
    return this.entries.slice().sort((a, b) => b.dateAction.localeCompare(a.dateAction));
  }
}
