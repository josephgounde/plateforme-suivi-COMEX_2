// Barres horizontales (ng-apexcharts), pour les répartitions par acteur où
// RepartitionChartComponent (barres CSS maison, sans axe) est trop sommaire.

import { Component, Input } from '@angular/core';
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
  @Input({ required: true }) titre!: string;
  @Input({ required: true }) items: BarChartItem[] = [];
  @Input() couleur = '#e30613'; // var(--pdoe-red) — ApexCharts n'accepte pas les custom properties CSS

  get chartOptions(): ChartOptions {
    return {
      series: [{ name: this.titre, data: this.items.map(i => i.valeur) }],
      chart: { type: 'bar', height: Math.max(220, this.items.length * 46), toolbar: { show: false } },
      xaxis: { categories: this.items.map(i => i.libelle) },
      dataLabels: { enabled: true, style: { fontSize: '12px', fontWeight: 700 } },
      plotOptions: {
        bar: { horizontal: true, borderRadius: 4, distributed: false, barHeight: '55%' }
      },
      grid: { borderColor: '#f0f0f0' },
      tooltip: { enabled: true },
      colors: [this.couleur]
    };
  }
}
