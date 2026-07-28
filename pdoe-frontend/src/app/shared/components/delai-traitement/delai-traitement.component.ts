// Compte à rebours avant échéance (Gestionnaire/COMEX/Trésorerie). Se rafraîchit toutes les 30s. dateDebut = dateDerniereAction, pas updatedAt.

import { ChangeDetectorRef, Component, Input, OnChanges, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

export type EtatDelai = 'ok' | 'attention' | 'depasse';

const INTERVALLE_RAFRAICHISSEMENT_MS = 30000;
// En-deçà de ce ratio de temps restant / délai total, l'état bascule
// en "attention" (orange) plutôt que "ok" (vert).
const SEUIL_ATTENTION_RATIO = 0.25;

@Component({
  selector: 'app-delai-traitement',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './delai-traitement.component.html',
  styleUrl: './delai-traitement.component.scss'
})
export class DelaiTraitementComponent implements OnInit, OnChanges, OnDestroy {
  // Date d'entrée dans l'étape courante (Dossier.dateDerniereAction).
  @Input({ required: true }) dateDebut!: string;

  // Délai imparti en heures pour cette étape (ParametreMetier.valeur
  // de DELAI_GESTIONNAIRE_HEURES / DELAI_COMEX_HEURES / DELAI_TRESORERIE_HEURES).
  @Input({ required: true }) delaiHeures!: number;

  libelle = '';
  etat: EtatDelai = 'ok';

  private minuteur?: ReturnType<typeof setInterval>;

  constructor(private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.recalculer();
    // Zoneless (pas de zone.js) : setInterval ne déclenche aucune détection de changement, d'où le detectChanges() explicite.
    this.minuteur = setInterval(() => {
      this.recalculer();
      this.cdr.detectChanges();
    }, INTERVALLE_RAFRAICHISSEMENT_MS);
  }

  ngOnChanges(): void {
    this.recalculer();
  }

  ngOnDestroy(): void {
    if (this.minuteur) clearInterval(this.minuteur);
  }

  private recalculer(): void {
    if (!this.dateDebut || !this.delaiHeures) return;

    const heuresEcoulees = (Date.now() - new Date(this.dateDebut).getTime()) / 3600000;
    const heuresRestantes = this.delaiHeures - heuresEcoulees;

    if (heuresRestantes <= 0) {
      this.etat = 'depasse';
      this.libelle = `Dépassé de ${this.formaterDuree(-heuresRestantes)}`;
    } else {
      this.etat = heuresRestantes / this.delaiHeures < SEUIL_ATTENTION_RATIO ? 'attention' : 'ok';
      this.libelle = `${this.formaterDuree(heuresRestantes)} restantes`;
    }
  }

  private formaterDuree(heures: number): string {
    const h = Math.floor(heures);
    const min = Math.round((heures - h) * 60);
    if (h === 0) return `${min}min`;
    return `${h}h${min > 0 ? String(min).padStart(2, '0') : ''}`;
  }
}
