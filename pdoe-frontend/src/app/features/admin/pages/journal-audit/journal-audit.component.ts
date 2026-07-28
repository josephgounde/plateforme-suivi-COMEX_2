// Journal d'audit admin/sécurité (dbo.JournalAudit), pas le cycle de vie
// des dossiers, déjà couvert par l'historique par dossier.

import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { DossierApiService } from '../../../../core/api/dossier-api.service';
import { JournalAuditEntry } from '../../../../core/models/dossier.model';
import { CategorieAudit, CATEGORIE_AUDIT_LABELS } from '../../../../core/models/enums.model';
import { DropdownSelectComponent, DropdownOption } from '../../../../shared/components/dropdown-select/dropdown-select.component';

@Component({
  selector: 'app-journal-audit',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, DropdownSelectComponent],
  templateUrl: './journal-audit.component.html',
  styleUrl: './journal-audit.component.scss'
})
export class JournalAuditComponent implements OnInit {
  chargement = true;
  erreur = '';
  entrees: JournalAuditEntry[] = [];
  filtreCategorie: CategorieAudit | 'TOUTES' = 'TOUTES';
  recherche = '';

  readonly CategorieAudit = CategorieAudit;
  readonly CATEGORIE_AUDIT_LABELS = CATEGORIE_AUDIT_LABELS;

  readonly categorieOptions: DropdownOption[] = [
    { value: 'TOUTES', label: 'Toutes les catégories' },
    { value: CategorieAudit.AUTHENTIFICATION, label: CATEGORIE_AUDIT_LABELS[CategorieAudit.AUTHENTIFICATION] },
    { value: CategorieAudit.UTILISATEUR, label: CATEGORIE_AUDIT_LABELS[CategorieAudit.UTILISATEUR] },
    { value: CategorieAudit.PARAMETRAGE, label: CATEGORIE_AUDIT_LABELS[CategorieAudit.PARAMETRAGE] },
    { value: CategorieAudit.WORKFLOW, label: CATEGORIE_AUDIT_LABELS[CategorieAudit.WORKFLOW] }
  ];

  constructor(
    private api: DossierApiService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.charger();
  }

  private charger(): void {
    this.chargement = true;
    this.api.getJournalAudit().subscribe({
      next: e => {
        this.entrees = e;
        this.chargement = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.erreur = 'Échec du chargement du journal.';
        this.chargement = false;
        this.cdr.detectChanges();
      }
    });
  }

  get entreesFiltrees(): JournalAuditEntry[] {
    let resultat = this.entrees;
    if (this.filtreCategorie !== 'TOUTES') {
      resultat = resultat.filter(e => e.categorie === this.filtreCategorie);
    }
    const terme = this.recherche.trim().toLowerCase();
    if (terme) {
      resultat = resultat.filter(e =>
        e.description.toLowerCase().includes(terme) || e.createdBy.toLowerCase().includes(terme)
      );
    }
    return resultat;
  }
}
