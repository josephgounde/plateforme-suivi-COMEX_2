// Vue générique de /dossiers, écran de recherche plutôt que dashboard :
// liste plate de tout ce que l'API renvoie pour le profil, filtrée côté client.

import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DossierApiService } from '../../../../core/api/dossier-api.service';
import { Dossier } from '../../../../core/models/dossier.model';
import { DropdownSelectComponent, DropdownOption } from '../../../../shared/components/dropdown-select/dropdown-select.component';
import { PagerComponent } from '../../../../shared/components/pager/pager.component';
import {
  StatutDossier,
  TypeOperation,
  STATUT_LABELS,
  TYPE_OPERATION_LABELS
} from '../../../../core/models/enums.model';

const PAGE_SIZE = 10;

@Component({
  selector: 'app-dossier-list',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, DropdownSelectComponent, PagerComponent],
  templateUrl: './dossier-list.component.html',
  styleUrl: './dossier-list.component.scss'
})
export class DossierListComponent implements OnInit {
  chargement = true;
  tousLesDossiers: Dossier[] = [];

  recherche = '';
  filtreStatut: StatutDossier | '' = '';
  filtreType: TypeOperation | '' = '';

  readonly statuts = Object.values(StatutDossier);
  readonly types = Object.values(TypeOperation);
  readonly statutLabels = STATUT_LABELS;
  readonly typeLabels = TYPE_OPERATION_LABELS;

  readonly statutOptions: DropdownOption[] = [
    { value: '', label: 'Tous les statuts' },
    ...this.statuts.map(s => ({ value: s, label: this.statutLabels[s] }))
  ];
  readonly typeOptions: DropdownOption[] = [
    { value: '', label: 'Tous les types' },
    ...this.types.map(t => ({ value: t, label: this.typeLabels[t] }))
  ];

  constructor(
    private dossierApi: DossierApiService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.dossierApi.listDossiers({ pageSize: 200 }).subscribe({
      next: reponse => {
        this.tousLesDossiers = reponse.items;
        this.chargement = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.chargement = false;
        this.cdr.detectChanges();
      }
    });
  }

  get dossiersFiltres(): Dossier[] {
    const texte = this.recherche.trim().toLowerCase();

    return this.tousLesDossiers.filter(d => {
      const matchTexte =
        !texte ||
        d.referenceInterne.toLowerCase().includes(texte) ||
        d.nomClient.toLowerCase().includes(texte) ||
        d.numCompte.toLowerCase().includes(texte);

      const matchStatut = !this.filtreStatut || d.statutElectronique === this.filtreStatut;
      const matchType = !this.filtreType || d.typeOperation === this.filtreType;

      return matchTexte && matchStatut && matchType;
    });
  }

  reinitialiserFiltres(): void {
    this.recherche = '';
    this.filtreStatut = '';
    this.filtreType = '';
    this.page = 1;
  }

  readonly pageSize = PAGE_SIZE;
  page = 1;

  get dossiersFiltresPage(): Dossier[] {
    const liste = this.dossiersFiltres;
    const totalPages = Math.max(1, Math.ceil(liste.length / PAGE_SIZE));
    if (this.page > totalPages) this.page = totalPages;
    const debut = (this.page - 1) * PAGE_SIZE;
    return liste.slice(debut, debut + PAGE_SIZE);
  }
}