// Items de la checklist d'apurement : réordonnancement, activation et ajout
// sans déploiement. Même pattern que WorkflowEtapesComponent, en plus simple.

import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ChecklistConfigApiService } from '../../../../core/api/checklist-config-api.service';
import { ChecklistItemConfig } from '../../../../core/models/dossier.model';

@Component({
  selector: 'app-checklist-config',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './checklist-config.component.html',
  styleUrl: './checklist-config.component.scss'
})
export class ChecklistConfigComponent implements OnInit {
  chargement = true;
  items: ChecklistItemConfig[] = [];
  reordonnancementEnCours = false;
  toggleEnCours = new Set<number>();
  erreur = '';

  afficherFormulaireAjout = false;
  nouveauLibelle = '';
  ajoutEnCours = false;
  erreurAjout = '';

  constructor(
    private api: ChecklistConfigApiService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.charger();
  }

  private charger(): void {
    this.chargement = true;
    this.api.list().subscribe({
      next: i => {
        this.items = i;
        this.chargement = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.chargement = false;
        this.cdr.detectChanges();
      }
    });
  }

  // ── Réordonnancement ──────────────────────────────────────────

  peutMonter(index: number): boolean {
    return index > 0;
  }

  peutDescendre(index: number): boolean {
    return index < this.items.length - 1;
  }

  monter(index: number): void {
    if (!this.peutMonter(index)) return;
    this.echangerEtReordonner(index, index - 1);
  }

  descendre(index: number): void {
    if (!this.peutDescendre(index)) return;
    this.echangerEtReordonner(index, index + 1);
  }

  private echangerEtReordonner(i: number, j: number): void {
    if (this.reordonnancementEnCours) return;
    const copie = this.items.slice();
    [copie[i], copie[j]] = [copie[j], copie[i]];

    this.reordonnancementEnCours = true;
    this.erreur = '';
    this.api.reordonner(copie.map(x => x.checklistItemId)).subscribe({
      next: i => {
        this.items = i;
        this.reordonnancementEnCours = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.reordonnancementEnCours = false;
        this.erreur = 'Échec du réordonnancement.';
        this.cdr.detectChanges();
      }
    });
  }

  // ── Activation / désactivation ──────────────────────────────────

  basculerActif(item: ChecklistItemConfig): void {
    if (this.toggleEnCours.has(item.checklistItemId)) return;

    this.toggleEnCours.add(item.checklistItemId);
    this.erreur = '';
    this.api.modifier(item.checklistItemId, { actif: !item.actif }).subscribe({
      next: maj => {
        const idx = this.items.findIndex(x => x.checklistItemId === item.checklistItemId);
        if (idx >= 0) this.items[idx] = maj;
        this.toggleEnCours.delete(item.checklistItemId);
        this.cdr.detectChanges();
      },
      error: () => {
        this.toggleEnCours.delete(item.checklistItemId);
        this.erreur = "Échec de la mise à jour de l'item.";
        this.cdr.detectChanges();
      }
    });
  }

  // ── Ajout d'item ─────────────────────────────────────────────

  ouvrirFormulaireAjout(): void {
    this.afficherFormulaireAjout = true;
    this.nouveauLibelle = '';
    this.erreurAjout = '';
  }

  annulerAjout(): void {
    this.afficherFormulaireAjout = false;
  }

  ajoutValide(): boolean {
    return this.nouveauLibelle.trim().length > 0;
  }

  ajouter(): void {
    if (!this.ajoutValide() || this.ajoutEnCours) return;

    this.ajoutEnCours = true;
    this.erreurAjout = '';
    this.api.creer({ libelle: this.nouveauLibelle.trim() }).subscribe({
      next: () => {
        this.ajoutEnCours = false;
        this.afficherFormulaireAjout = false;
        this.charger();
      },
      error: () => {
        this.ajoutEnCours = false;
        this.erreurAjout = "Échec de la création.";
        this.cdr.detectChanges();
      }
    });
  }
}
