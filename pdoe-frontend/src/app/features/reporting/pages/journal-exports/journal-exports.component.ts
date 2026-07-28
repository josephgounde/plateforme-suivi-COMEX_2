// Journal des exports (dbo.ExportsReglementaires) — historique de tout export généré depuis Reporting,
// réglementaire (gabarits officiels DGI/Trésor/BCEAO) et opérationnel (interne), avec re-téléchargement.

import { Component, ChangeDetectorRef, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportingApiService } from '../../../../core/api/reporting-api.service';
import { ExportReglementaire } from '../../../../core/models/dossier.model';
import {
  CategorieExport, TypeExport, CATEGORIE_EXPORT_LABELS, TYPE_EXPORT_LABELS
} from '../../../../core/models/enums.model';
import { DropdownSelectComponent, DropdownOption } from '../../../../shared/components/dropdown-select/dropdown-select.component';
import { PagerComponent } from '../../../../shared/components/pager/pager.component';
import { declencherTelechargement } from '../../../../shared/utils/telechargement.util';

@Component({
  selector: 'app-journal-exports',
  standalone: true,
  imports: [CommonModule, FormsModule, DropdownSelectComponent, PagerComponent],
  templateUrl: './journal-exports.component.html',
  styleUrl: './journal-exports.component.scss'
})
export class JournalExportsComponent implements OnInit {
  chargement = true;
  erreur = '';
  entrees: ExportReglementaire[] = [];
  total = 0;
  page = 1;
  readonly pageSize = 20;

  filtreCategorie: CategorieExport | 'TOUTES' = 'TOUTES';
  filtreTypeExport: TypeExport | 'TOUS' = 'TOUS';
  filtreDateDebut = '';
  filtreDateFin = '';

  // Suivi par ligne — évite qu'un double-clic (ou un double-dispatch du preview) relance deux fois le même téléchargement.
  telechargementsEnCours = new Set<number>();

  readonly CATEGORIE_EXPORT_LABELS = CATEGORIE_EXPORT_LABELS;
  readonly TYPE_EXPORT_LABELS = TYPE_EXPORT_LABELS;

  readonly categorieOptions: DropdownOption[] = [
    { value: 'TOUTES', label: 'Toutes les catégories' },
    { value: CategorieExport.REGLEMENTAIRE, label: CATEGORIE_EXPORT_LABELS[CategorieExport.REGLEMENTAIRE] },
    { value: CategorieExport.OPERATIONNEL, label: CATEGORIE_EXPORT_LABELS[CategorieExport.OPERATIONNEL] }
  ];

  readonly typeExportOptions: DropdownOption[] = [
    { value: 'TOUS', label: 'Tous les types' },
    ...Object.values(TypeExport).map(t => ({ value: t, label: TYPE_EXPORT_LABELS[t] }))
  ];

  constructor(
    private api: ReportingApiService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.charger();
  }

  private charger(): void {
    this.chargement = true;
    this.erreur = '';
    this.api.getJournalExports({
      categorie: this.filtreCategorie === 'TOUTES' ? undefined : this.filtreCategorie,
      typeExport: this.filtreTypeExport === 'TOUS' ? undefined : this.filtreTypeExport,
      dateDebut: this.filtreDateDebut || undefined,
      dateFin: this.filtreDateFin || undefined,
      page: this.page,
      pageSize: this.pageSize
    }).subscribe({
      next: reponse => {
        this.entrees = reponse.items;
        this.total = reponse.total;
        this.chargement = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.erreur = 'Échec du chargement du journal des exports.';
        this.chargement = false;
        this.cdr.detectChanges();
      }
    });
  }

  modifierDateDebut(valeur: string): void {
    this.filtreDateDebut = valeur;
    this.appliquerFiltres();
  }

  modifierDateFin(valeur: string): void {
    this.filtreDateFin = valeur;
    this.appliquerFiltres();
  }

  appliquerFiltres(): void {
    this.page = 1;
    this.charger();
  }

  formaterTaille(octets: number): string {
    if (octets < 1024) return `${octets} o`;
    const ko = octets / 1024;
    if (ko < 1024) return `${ko.toFixed(1)} Ko`;
    return `${(ko / 1024).toFixed(1)} Mo`;
  }

  changerPage(page: number): void {
    this.page = page;
    this.charger();
  }

  telecharger(export_: ExportReglementaire): void {
    if (this.telechargementsEnCours.has(export_.exportReglementaireId)) return;

    this.telechargementsEnCours.add(export_.exportReglementaireId);
    this.api.downloadJournalExport(export_.exportReglementaireId).subscribe({
      next: blob => {
        declencherTelechargement(blob, export_.nomFichier);
        this.telechargementsEnCours.delete(export_.exportReglementaireId);
        this.cdr.detectChanges();
      },
      error: () => {
        this.erreur = `Échec du téléchargement de ${export_.nomFichier}.`;
        this.telechargementsEnCours.delete(export_.exportReglementaireId);
        this.cdr.detectChanges();
      }
    });
  }
}
