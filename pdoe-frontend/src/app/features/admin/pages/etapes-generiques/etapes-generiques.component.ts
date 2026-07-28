// Écran de secours pour les dossiers sur une étape GENERIQUE (pas de profil propriétaire, réservé à Admin/Direction).
// Limite assumée : pas de cible de rejet configurable, toujours l'étape précédente.

import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { DossierApiService } from '../../../../core/api/dossier-api.service';
import { WorkflowApiService } from '../../../../core/api/workflow-api.service';
import { MockWorkflowConfigStore } from '../../../../core/mock/mock-workflow-config.store';
import { Dossier } from '../../../../core/models/dossier.model';
import { SousEtat } from '../../../../core/models/enums.model';

@Component({
  selector: 'app-etapes-generiques',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './etapes-generiques.component.html',
  styleUrl: './etapes-generiques.component.scss'
})
export class EtapesGeneriquesComponent implements OnInit {
  chargement = true;
  dossiers: Dossier[] = [];
  readonly SousEtat = SousEtat;

  rejetOuvert = new Set<number>();
  actionEnCours = new Set<number>();
  private motifsRejet = new Map<number, string>();

  constructor(
    private dossierApi: DossierApiService,
    private workflowApi: WorkflowApiService,
    private workflowConfig: MockWorkflowConfigStore,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.dossierApi.listDossiers().subscribe({
      next: reponse => {
        this.dossiers = reponse.items.filter(d => !!d.etapeGenerique);
        this.chargement = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.chargement = false;
        this.cdr.detectChanges();
      }
    });
  }

  libelleEtape(dossier: Dossier): string {
    const code = dossier.etapeGenerique?.etapeCode;
    if (!code) return '';
    return this.workflowConfig.findByCode(code)?.libelle ?? code;
  }

  motifRejet(dossierId: number): string {
    return this.motifsRejet.get(dossierId) ?? '';
  }

  modifierMotifRejet(dossierId: number, valeur: string): void {
    this.motifsRejet.set(dossierId, valeur);
  }

  basculerRejet(dossierId: number): void {
    if (this.rejetOuvert.has(dossierId)) {
      this.rejetOuvert.delete(dossierId);
    } else {
      this.rejetOuvert.add(dossierId);
    }
  }

  valider(dossier: Dossier): void {
    if (this.actionEnCours.has(dossier.dossierId) || !dossier.etapeGenerique) return;

    this.actionEnCours.add(dossier.dossierId);
    this.workflowApi
      .valider(dossier.dossierId, { niveauValidation: dossier.etapeGenerique.etapeCode })
      .subscribe({
        next: () => {
          // Le dossier a quitté cette étape (ou est passé sur une
          // autre étape GENERIQUE) — retiré de la liste dans les deux cas.
          this.dossiers = this.dossiers.filter(d => d.dossierId !== dossier.dossierId);
          this.actionEnCours.delete(dossier.dossierId);
          this.cdr.detectChanges();
        },
        error: () => {
          this.actionEnCours.delete(dossier.dossierId);
          this.cdr.detectChanges();
        }
      });
  }

  peutRejeter(dossierId: number): boolean {
    return this.motifRejet(dossierId).trim().length > 0;
  }

  rejeter(dossier: Dossier): void {
    if (this.actionEnCours.has(dossier.dossierId) || !this.peutRejeter(dossier.dossierId) || !dossier.etapeGenerique) return;

    this.actionEnCours.add(dossier.dossierId);
    this.workflowApi
      .rejeter(dossier.dossierId, {
        niveauValidation: dossier.etapeGenerique.etapeCode,
        motifRejet: this.motifRejet(dossier.dossierId),
        // Sans objet pour une étape générique, conservé pour satisfaire le type partagé RejeterEtapeRequest.
        responsableCorrection: dossier.etapeGenerique.etapeCode
      })
      .subscribe({
        next: () => {
          const idx = this.dossiers.findIndex(d => d.dossierId === dossier.dossierId);
          if (idx >= 0 && dossier.etapeGenerique) {
            this.dossiers[idx] = { ...dossier, etapeGenerique: { ...dossier.etapeGenerique, sousEtat: SousEtat.REJETE } };
          }
          this.rejetOuvert.delete(dossier.dossierId);
          this.actionEnCours.delete(dossier.dossierId);
          this.cdr.detectChanges();
        },
        error: () => {
          this.actionEnCours.delete(dossier.dossierId);
          this.cdr.detectChanges();
        }
      });
  }
}
