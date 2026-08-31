// Barres horizontales (ng-apexcharts), pour les répartitions par acteur où
// RepartitionChartComponent (barres CSS maison, sans axe) est trop sommaire.

import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  NgApexchartsModule,
  ApexAxisChartSeries,
  ApexChart,
  ApexXAxis,
  ApexDataLabels,
  ApexPlotOptions,
  ApexGrid,
  ApexTooltip
} from 'ng-apexcharts';
import { ThemeService } from '../../../core/theme/theme.service';
import { resolveToken } from '../../utils/resolve-token';

export interface BarChartItem {
  libelle: string;
  valeur: number;
}

export type ChartOptions = {
  series: ApexAxisChartSeries;
  chart: ApexChart;
  xaxis: ApexXAxis;
  dataLabels: ApexDataLabels;
  plotOptions: ApexPlotOptions;
  grid: ApexGrid;
  tooltip: ApexTooltip;
  colors: string[];
};

@Component({
  selector: 'app-bar-chart',
  standalone: true,
  imports: [CommonModule, NgApexchartsModule],
  templateUrl: './bar-chart.component.html',
  styleUrl: './bar-chart.component.scss'
})
export class BarChartComponent {
  private theme = inject(ThemeService);

  @Input({ required: true }) titre!: string;
  @Input({ required: true }) items: BarChartItem[] = [];
  // Nom de jeton (ex. '--pdoe-info') plutôt qu'un hex figé — cf. GaugeChartComponent pour le raisonnement complet.
  @Input() couleur = '--pdoe-red';

  get chartOptions(): ChartOptions {
    this.theme.theme(); // dépendance de signal : force le recalcul à la bascule de thème
    return {
      series: [{ name: this.titre, data: this.items.map(i => i.valeur) }],
      chart: { type: 'bar', height: Math.max(220, this.items.length * 46), toolbar: { show: false } },
      xaxis: { categories: this.items.map(i => i.libelle) },
      dataLabels: { enabled: true, style: { fontSize: '12px', fontWeight: 700 } },
      plotOptions: {
        bar: { horizontal: true, borderRadius: 4, distributed: false, barHeight: '55%' }
      },
      grid: { borderColor: resolveToken('--pdoe-border') },
      tooltip: { enabled: true },
      colors: [resolveToken(this.couleur)]
    };
  }
}
