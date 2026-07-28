// Jauge radiale (ng-apexcharts) pour un pourcentage borné [0, 100], ex. taux d'apurement.

import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgApexchartsModule, ApexChart, ApexPlotOptions, ApexFill, ApexStroke } from 'ng-apexcharts';

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
  @Input({ required: true }) titre!: string;
  @Input({ required: true }) valeur!: number; // 0-100
  @Input() couleur = '#e30613'; // var(--pdoe-red) — ApexCharts n'accepte pas les custom properties CSS

  get chartOptions(): ChartOptions {
    const valeur = this.valeur;
    return {
      series: [Math.max(0, Math.min(100, valeur))],
      chart: { type: 'radialBar', height: 260, sparkline: { enabled: false } },
      plotOptions: {
        radialBar: {
          hollow: { size: '62%' },
          track: { background: '#eeeeee' },
          dataLabels: {
            name: { show: false },
            value: {
              show: true,
              fontSize: '30px',
              fontWeight: 700,
              color: '#1a1a1a',
              offsetY: 10,
              formatter: () => `${valeur.toLocaleString('fr-FR')} %`
            }
          }
        }
      },
      fill: { type: 'solid' },
      stroke: { lineCap: 'round' },
      labels: [this.titre],
      colors: [this.couleur]
    };
  }
}
