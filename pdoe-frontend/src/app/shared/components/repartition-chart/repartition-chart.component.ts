// Barres horizontales colorées pour une répartition en catégories (étapes,
// devises, statuts…). Extrait d'AdminDashboardComponent, réutilisé par les autres dashboards.

import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface RepartitionItem {
  libelle: string;
  couleur: string;
  count: number;
  pourcentage: number;
  rejete?: boolean;
}

@Component({
  selector: 'app-repartition-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './repartition-chart.component.html',
  styleUrl: './repartition-chart.component.scss'
})
export class RepartitionChartComponent {
  @Input({ required: true }) titre!: string;
  @Input({ required: true }) items: RepartitionItem[] = [];
}
