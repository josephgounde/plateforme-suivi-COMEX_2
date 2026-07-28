// Rangée de chiffres clés dans une seule carte, remplace l'ancienne grille
// de cartes à icône sur les vues "Vue d'ensemble".

import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface MetriqueItem {
  libelle: string;
  valeur: string;
  accent?: 'success' | 'danger' | 'warning';
}

@Component({
  selector: 'app-metriques-bandeau',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './metriques-bandeau.component.html',
  styleUrl: './metriques-bandeau.component.scss'
})
export class MetriquesBandeauComponent {
  @Input({ required: true }) items: MetriqueItem[] = [];
}
