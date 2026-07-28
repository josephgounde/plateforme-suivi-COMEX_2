// Étapes du circuit configurables (dbo.WorkflowEtapes) : réordonnancement, activation, ajout sans déploiement.
// Boutons monter/descendre plutôt que drag-drop, pour éviter @angular/cdk pour ce seul écran.

import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { WorkflowConfigApiService } from '../../../../core/api/workflow-config-api.service';
import { EtapeWorkflowConfig } from '../../../../core/models/dossier.model';
import { NiveauValidation } from '../../../../core/models/enums.model';

@Component({
  selector: 'app-workflow-etapes',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './workflow-etapes.component.html',
  styleUrl: './workflow-etapes.component.scss'
})
export class WorkflowEtapesComponent implements OnInit {
  chargement = true;
  etapes: EtapeWorkflowConfig[] = [];
  reordonnancementEnCours = false;
  toggleEnCours = new Set<string>();
  erreur = '';

  afficherFormulaireAjout = false;
  nouveauCode = '';
  nouveauLibelle = '';
  nouvelOrdre = 1;
  ajoutEnCours = false;
  erreurAjout = '';

  readonly etapeInitiation = NiveauValidation.ETAPE_1_INITIATION as string;

  constructor(
    private api: WorkflowConfigApiService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.charger();
  }

  private charger(): void {
    this.chargement = true;
    this.api.list().subscribe({
      next: e => {
        this.etapes = e;
        this.nouvelOrdre = e.length + 1;
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
    return index < this.etapes.length - 1;
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
    const copie = this.etapes.slice();
    [copie[i], copie[j]] = [copie[j], copie[i]];

    this.reordonnancementEnCours = true;
    this.erreur = '';
    this.api.reordonner(copie.map(e => e.code)).subscribe({
      next: e => {
        this.etapes = e;
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

  basculerActif(etape: EtapeWorkflowConfig): void {
    if (this.toggleEnCours.has(etape.code) || etape.code === this.etapeInitiation) return;

    this.toggleEnCours.add(etape.code);
    this.erreur = '';
    this.api.modifier(etape.code, { actif: !etape.actif }).subscribe({
      next: maj => {
        const idx = this.etapes.findIndex(e => e.code === etape.code);
        if (idx >= 0) this.etapes[idx] = maj;
        this.toggleEnCours.delete(etape.code);
        this.cdr.detectChanges();
      },
      error: () => {
        this.toggleEnCours.delete(etape.code);
        this.erreur = "Échec de la mise à jour de l'étape.";
        this.cdr.detectChanges();
      }
    });
  }

  // ── Ajout d'étape personnalisée ─────────────────────────────────

  ouvrirFormulaireAjout(): void {
    this.afficherFormulaireAjout = true;
    this.nouveauCode = '';
    this.nouveauLibelle = '';
    this.nouvelOrdre = this.etapes.length + 1;
    this.erreurAjout = '';
  }

  annulerAjout(): void {
    this.afficherFormulaireAjout = false;
  }

  ajoutValide(): boolean {
    return /^[A-Z0-9_]+$/.test(this.nouveauCode.trim()) &&
      this.nouveauLibelle.trim().length > 0 &&
      this.nouvelOrdre >= 1;
  }

  ajouter(): void {
    if (!this.ajoutValide() || this.ajoutEnCours) return;

    this.ajoutEnCours = true;
    this.erreurAjout = '';
    this.api.creer({
      code: this.nouveauCode.trim(),
      libelle: this.nouveauLibelle.trim(),
      ordre: this.nouvelOrdre
    }).subscribe({
      next: () => {
        this.ajoutEnCours = false;
        this.afficherFormulaireAjout = false;
        this.charger();
      },
      error: () => {
        this.ajoutEnCours = false;
        this.erreurAjout = "Échec de la création — ce code est peut-être déjà utilisé.";
        this.cdr.detectChanges();
      }
    });
  }
}
