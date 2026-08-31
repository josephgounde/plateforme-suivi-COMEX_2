// Jauge radiale (ng-apexcharts) pour un pourcentage borné [0, 100], ex. taux d'apurement.

import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgApexchartsModule, ApexChart, ApexPlotOptions, ApexFill, ApexStroke } from 'ng-apexcharts';
import { ThemeService } from '../../../core/theme/theme.service';
import { resolveToken } from '../../utils/resolve-token';

export type ChartOptions = {
  series: number[];
  chart: ApexChart;
  plotOptions: ApexPlotOptions;
  fill: ApexFill;
  stroke: ApexStroke;
  labels: string[];
  colors: string[];
};

@Component({
  selector: 'app-gauge-chart',
  standalone: true,
  imports: [CommonModule, NgApexchartsModule],
  templateUrl: './gauge-chart.component.html',
  styleUrl: './gauge-chart.component.scss'
})
export class GaugeChartComponent {
  private theme = inject(ThemeService);

  @Input({ required: true }) titre!: string;
  @Input({ required: true }) valeur!: number; // 0-100
  // Nom de jeton (ex. '--pdoe-success-strong') plutôt qu'un hex figé : ApexCharts n'accepte pas les custom
  // properties CSS directement, donc on résout la valeur réelle nous-mêmes (cf. resolveToken) — ce qui permet
  // au passage de suivre le thème clair/sombre au lieu de rester bloqué sur la couleur du premier rendu.
  @Input() couleur = '--pdoe-red';

  get chartOptions(): ChartOptions {
    this.theme.theme(); // dépendance de signal : force le recalcul à la bascule de thème
    const valeur = this.valeur;
    return {
      series: [Math.max(0, Math.min(100, valeur))],
      chart: { type: 'radialBar', height: 260, sparkline: { enabled: false } },
      plotOptions: {
        radialBar: {
          hollow: { size: '62%' },
          track: { background: resolveToken('--pdoe-surface-hover') },
          dataLabels: {
            name: { show: false },
            value: {
              show: true,
              fontSize: '30px',
              fontWeight: 700,
              color: resolveToken('--pdoe-ink'),
              offsetY: 10,
              formatter: () => `${valeur.toLocaleString('fr-FR')} %`
            }
          }
        }
      },
      fill: { type: 'solid' },
      stroke: { lineCap: 'round' },
      labels: [this.titre],
      colors: [resolveToken(this.couleur)]
    };
  }
}
