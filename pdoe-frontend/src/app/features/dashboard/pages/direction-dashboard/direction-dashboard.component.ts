// Dashboard Direction : vue transversale en lecture seule, sauf lever l'alerte anti-fractionnement et rejet définitif.
// Trois blocs : métriques agrégées, dossiers en retard, tous les dossiers.

import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ReportingApiService } from '../../../../core/api/reporting-api.service';
import { DossierApiService } from '../../../../core/api/dossier-api.service';
import { WorkflowApiService } from '../../../../core/api/workflow-api.service';
import { WorkflowStepperComponent } from '../../../../shared/components/workflow-stepper/workflow-stepper.component';
import { RepartitionChartComponent, RepartitionItem } from '../../../../shared/components/repartition-chart/repartition-chart.component';
import { MetriquesBandeauComponent, MetriqueItem } from '../../../../shared/components/metriques-bandeau/metriques-bandeau.component';
import { GaugeChartComponent } from '../../../../shared/components/gauge-chart/gauge-chart.component';
import { BarChartComponent, BarChartItem } from '../../../../shared/components/bar-chart/bar-chart.component';
import { declencherTelechargement } from '../../../../shared/utils/telechargement.util';
import { NotificationPanelComponent } from '../../../../shared/components/notification-panel/notification-panel.component';
import { NotificationsService } from '../../../../core/notifications/notifications.service';
import {
  DashboardData,
  DossierRetard,
  Dossier
} from '../../../../core/models/dossier.model';
import {
  StatutDossier,
  TypeOperation,
  STATUT_LABELS,
  TYPE_OPERATION_LABELS
} from '../../../../core/models/enums.model';
import { DashboardNavService } from '../../../../core/layout/dashboard-nav.service';
import { PagerComponent } from '../../../../shared/components/pager/pager.component';
import { DropdownSelectComponent, DropdownOption } from '../../../../shared/components/dropdown-select/dropdown-select.component';

const PAGE_SIZE = 10;

// Statuts où l'opération a réellement eu lieu (ou est close) — un rejet définitif n'a plus de sens passé ce point.
const STATUTS_DEJA_EXECUTES: ReadonlySet<StatutDossier> = new Set([
  StatutDossier.EXECUTE,
  StatutDossier.EN_APUREMENT,
  StatutDossier.APUREMENT_PARTIEL,
  StatutDossier.ALERTE_J14,
  StatutDossier.ALERTE_J8,
  StatutDossier.DEPASSE_BCEAO,
  StatutDossier.APURE,
  StatutDossier.EN_ARCHIVAGE,
  StatutDossier.ARCHIVE
]);

// Un module = un onglet de la barre latérale qui remplace le contenu affiché, pas un ancrage de défilement.
type VueDirection = 'repartition' | 'goulots' | 'tous-dossiers';

interface ModuleNav {
  vue: VueDirection;
  icone: string;
  libelle: string;
  count: number;
}

@Component({
  selector: 'app-direction-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    WorkflowStepperComponent,
    RepartitionChartComponent,
    MetriquesBandeauComponent,
    GaugeChartComponent,
    BarChartComponent,
    NotificationPanelComponent,
    PagerComponent,
    DropdownSelectComponent
  ],
  templateUrl: './direction-dashboard.component.html',
  styleUrl: './direction-dashboard.component.scss'
})
export class DirectionDashboardComponent implements OnInit, OnDestroy {
  chargement = true;
  dashboard: DashboardData | null = null;
  dossiersEnRetard: DossierRetard[] = [];
  tousLesDossiers: Dossier[] = [];
  exportRetardEnCours = false;
  exportRapportEnCours = false;

  // ── Exports réglementaires (DGI / Trésor / BCEAO) — gabarits officiels remplis côté backend (PDOE.Reporting.API).
  reglementaireDateDebut = this.premierJourDuMois();
  reglementaireDateFin = this.aujourdHui();
  exportCrpiDgiEnCours = false;
  exportCrpiTresorEnCours = false;
  exportSituationBceaoEnCours = false;

  readonly statutLabels = STATUT_LABELS;
  readonly typeLabels = TYPE_OPERATION_LABELS;

  readonly StatutDossier = StatutDossier;

  // ── Recherche / filtres — "Tous les dossiers", même pattern que DossierListComponent.
  recherche = '';
  filtreStatut: StatutDossier | '' = '';
  filtreType: TypeOperation | '' = '';

  readonly statutOptions: DropdownOption[] = [
    { value: '', label: 'Tous les statuts' },
    ...Object.values(StatutDossier).map(s => ({ value: s, label: this.statutLabels[s] }))
  ];
  readonly typeOptions: DropdownOption[] = [
    { value: '', label: 'Tous les types' },
    ...Object.values(TypeOperation).map(t => ({ value: t, label: this.typeLabels[t] }))
  ];

  // ── Alerte anti-fractionnement + rejet définitif ── le contrôle est porté par le COMEX, mais la Direction garde un droit d'override (comme Admin DSIRI).
  leverAlerteEnCours = new Set<number>();
  rejetDefOuvert = new Set<number>();
  rejetDefEnCours = new Set<number>();
  private motifsRejetDef = new Map<number, string>();

  constructor(
    private reporting: ReportingApiService,
    private dossierApi: DossierApiService,
    private workflowApi: WorkflowApiService,
    private cdr: ChangeDetectorRef,
    private dashboardNav: DashboardNavService,
    public notifs: NotificationsService
  ) {}

  // Onglet actif — porté par DashboardNavService, cf.
  // AgentAccueilDashboardComponent pour le raisonnement complet.
  get vueActive(): VueDirection {
    return (this.dashboardNav.activeId() as VueDirection | null) ?? 'repartition';
  }

  ngOnInit(): void {
    this.notifs.charger();
    this.dashboardNav.select('repartition');
    this.dashboardNav.setItems(this.versNavItems());

    this.reporting.getDashboard().subscribe({
      next: data => {
        this.dashboard = data;
        this.chargement = false;
        this.dashboardNav.setItems(this.versNavItems());
        this.cdr.detectChanges();
      },
      error: () => {
        this.chargement = false;
        this.cdr.detectChanges();
      }
    });

    this.reporting.getDossiersEnRetard().subscribe({
      next: retards => {
        this.dossiersEnRetard = retards;
        this.dashboardNav.setItems(this.versNavItems());
        this.cdr.detectChanges();
      }
    });

    this.dossierApi.listDossiers({ pageSize: 200 }).subscribe({
      next: reponse => {
        this.tousLesDossiers = reponse.items;
        this.dashboardNav.setItems(this.versNavItems());
        this.cdr.detectChanges();
      }
    });
  }

  ngOnDestroy(): void {
    this.dashboardNav.clear();
  }

  private versNavItems() {
    return this.modulesNav.map(m => ({ id: m.vue, icone: m.icone, libelle: m.libelle, count: m.count }));
  }

  get metriquesApercu(): MetriqueItem[] {
    if (!this.dashboard) return [];
    const items: MetriqueItem[] = [
      { libelle: 'Dossiers actifs', valeur: String(this.dashboard.totalDossiers) }
    ];
    if (this.dashboard.dossiersEnRetard > 0) {
      items.push({ libelle: 'En retard', valeur: String(this.dashboard.dossiersEnRetard), accent: 'danger' });
    }
    if (this.dashboard.dossiersApurementProche > 0) {
      items.push({ libelle: 'Échéance < 30 j', valeur: String(this.dashboard.dossiersApurementProche), accent: 'warning' });
    }
    items.push({ libelle: "Taux d'apurement", valeur: `${Math.round(this.dashboard.tauxApurement * 100)} %`, accent: 'success' });
    if (this.dashboard.alertesNonTraitees > 0) {
      items.push({ libelle: 'Alertes actives', valeur: String(this.dashboard.alertesNonTraitees), accent: 'danger' });
    }
    return items;
  }

  get tauxApurementPourcent(): number {
    return this.dashboard ? Math.round(this.dashboard.tauxApurement * 100) : 0;
  }

  // Comptage client-side (pas d'endpoint dédié), même dataset que "Tous les dossiers" mais agrégé par gestionnaireAssigne.
  get dossiersParGestionnaire(): BarChartItem[] {
    const compteurs = new Map<string, number>();
    for (const d of this.tousLesDossiers) {
      if (!d.gestionnaireAssigne) continue;
      compteurs.set(d.gestionnaireAssigne, (compteurs.get(d.gestionnaireAssigne) ?? 0) + 1);
    }
    return [...compteurs.entries()]
      .map(([libelle, valeur]) => ({ libelle, valeur }))
      .sort((a, b) => b.valeur - a.valeur);
  }

  get parStatutEntries(): { statut: StatutDossier; count: number }[] {
    if (!this.dashboard) return [];
    return Object.entries(this.dashboard.parStatut)
      .map(([statut, count]) => ({ statut: statut as StatutDossier, count }))
      .filter(e => e.count > 0)
      .sort((a, b) => b.count - a.count);
  }

  get maxParStatut(): number {
    return Math.max(1, ...this.parStatutEntries.map(e => e.count));
  }

  // Reformate parStatutEntries pour <app-repartition-chart> — mêmes seuils danger/warning/success que statutClass().
  get repartitionParStatut(): RepartitionItem[] {
    const max = this.maxParStatut;
    return this.parStatutEntries.map(e => ({
      libelle: this.statutLabels[e.statut],
      couleur: this.couleurStatut(e.statut),
      count: e.count,
      pourcentage: Math.round((e.count / max) * 100)
    }));
  }

  private couleurStatut(s: StatutDossier): string {
    const classe = this.statutClass(s);
    if (classe === 'badge--danger') return 'var(--pdoe-red)';
    if (classe === 'badge--warning') return '#e65100';
    if (classe === 'badge--success') return '#2e7d32';
    return '#1565c0';
  }

  // Toujours les 3 modules, même à 0 : ce sont de vrais onglets de navigation, pas des ancres de défilement.
  get modulesNav(): ModuleNav[] {
    return [
      { vue: 'repartition', icone: '📊', libelle: "Vue d'ensemble", count: this.parStatutEntries.length },
      { vue: 'goulots', icone: '🚧', libelle: "Goulots d'étranglement", count: this.dossiersEnRetard.length },
      { vue: 'tous-dossiers', icone: '📁', libelle: 'Tous les dossiers', count: this.tousLesDossiers.length }
    ];
  }

  readonly pageSize = PAGE_SIZE;
  pageTousDossiers = 1;

  get dossiersFiltres(): Dossier[] {
    const texte = this.recherche.trim().toLowerCase();

    return this.tousLesDossiers.filter(d => {
      const matchTexte =
        !texte ||
        d.referenceInterne.toLowerCase().includes(texte) ||
        d.nomClient.toLowerCase().includes(texte) ||
        d.numCompte.toLowerCase().includes(texte);

      const matchStatut = !this.filtreStatut || d.statutElectronique === this.filtreStatut;
      const matchType = !this.filtreType || d.typeOperation === this.filtreType;

      return matchTexte && matchStatut && matchType;
    });
  }

  reinitialiserFiltres(): void {
    this.recherche = '';
    this.filtreStatut = '';
    this.filtreType = '';
    this.pageTousDossiers = 1;
  }

  get tousLesDossiersAffiches(): Dossier[] {
    const liste = this.dossiersFiltres;
    const totalPages = Math.max(1, Math.ceil(liste.length / PAGE_SIZE));
    if (this.pageTousDossiers > totalPages) this.pageTousDossiers = totalPages;
    const debut = (this.pageTousDossiers - 1) * PAGE_SIZE;
    return liste.slice(debut, debut + PAGE_SIZE);
  }

  urgenceClass(d: DossierRetard): string {
    if (d.seuilDepasse >= 100) return 'urgence--critique';
    if (d.seuilDepasse >= 90) return 'urgence--haute';
    return 'urgence--normale';
  }

  exporterDossiersEnRetard(): void {
    if (this.exportRetardEnCours) return;

    this.exportRetardEnCours = true;
    this.reporting.exportDossiersEnRetard().subscribe({
      next: blob => {
        const date = new Date().toISOString().slice(0, 10);
        declencherTelechargement(blob, `dossiers-en-retard_${date}.xlsx`);
        this.exportRetardEnCours = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.exportRetardEnCours = false;
        this.cdr.detectChanges();
      }
    });
  }

  exporterRapportActivite(): void {
    if (this.exportRapportEnCours) return;

    this.exportRapportEnCours = true;
    const mois = new Date().toISOString().slice(0, 7);
    this.reporting.exportRapportActiviteMensuel(mois).subscribe({
      next: blob => {
        declencherTelechargement(blob, `rapport-activite_${mois}.xlsx`);
        this.exportRapportEnCours = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.exportRapportEnCours = false;
        this.cdr.detectChanges();
      }
    });
  }

  private aujourdHui(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private premierJourDuMois(): string {
    const d = new Date();
    return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().slice(0, 10);
  }

  modifierReglementaireDateDebut(valeur: string): void {
    this.reglementaireDateDebut = valeur;
  }

  modifierReglementaireDateFin(valeur: string): void {
    this.reglementaireDateFin = valeur;
  }

  exporterCrpiDgi(): void {
    if (this.exportCrpiDgiEnCours) return;

    this.exportCrpiDgiEnCours = true;
    this.reporting.exportCrpiDgi(this.reglementaireDateDebut, this.reglementaireDateFin).subscribe({
      next: blob => {
        declencherTelechargement(blob, `crpi-dgi_${this.reglementaireDateDebut}_${this.reglementaireDateFin}.xlsx`);
        this.exportCrpiDgiEnCours = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.exportCrpiDgiEnCours = false;
        this.cdr.detectChanges();
      }
    });
  }

  exporterCrpiTresor(): void {
    if (this.exportCrpiTresorEnCours) return;

    this.exportCrpiTresorEnCours = true;
    this.reporting.exportCrpiTresor(this.reglementaireDateDebut, this.reglementaireDateFin).subscribe({
      next: blob => {
        declencherTelechargement(blob, `crpi-tresor_${this.reglementaireDateDebut}_${this.reglementaireDateFin}.xlsx`);
        this.exportCrpiTresorEnCours = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.exportCrpiTresorEnCours = false;
        this.cdr.detectChanges();
      }
    });
  }

  exporterSituationBceao(): void {
    if (this.exportSituationBceaoEnCours) return;

    this.exportSituationBceaoEnCours = true;
    this.reporting.exportSituationBceao(this.reglementaireDateDebut, this.reglementaireDateFin).subscribe({
      next: blob => {
        declencherTelechargement(blob, `situation-bceao_${this.reglementaireDateDebut}_${this.reglementaireDateFin}.xlsx`);
        this.exportSituationBceaoEnCours = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.exportSituationBceaoEnCours = false;
        this.cdr.detectChanges();
      }
    });
  }

  statutClass(s: StatutDossier): string {
    const danger = [
      StatutDossier.ALERTE_J8,
      StatutDossier.DEPASSE_BCEAO,
      StatutDossier.REJETE_DEFINITIF,
      StatutDossier.ANTI_FRACTIONNEMENT_DETECTE
    ];
    const warning = [StatutDossier.ALERTE_J14, StatutDossier.EN_EXECUTION_SWIFT];
    const success = [StatutDossier.APURE, StatutDossier.ARCHIVE];
    if (danger.includes(s)) return 'badge--danger';
    if (warning.includes(s)) return 'badge--warning';
    if (success.includes(s)) return 'badge--success';
    return 'badge--default';
  }

  leverAlerte(dossier: Dossier): void {
    if (this.leverAlerteEnCours.has(dossier.dossierId)) return;

    this.leverAlerteEnCours.add(dossier.dossierId);
    this.workflowApi.leverAlerte(dossier.dossierId).subscribe({
      next: reponse => {
        dossier.statutElectronique = reponse.statutApres;
        this.leverAlerteEnCours.delete(dossier.dossierId);
        this.cdr.detectChanges();
      },
      error: () => {
        this.leverAlerteEnCours.delete(dossier.dossierId);
        this.cdr.detectChanges();
      }
    });
  }

  // Une fois exécutée, il n'y a plus rien à rejeter (l'argent est déjà parti côté SWIFT/ABS2000).
  peutRejeterDefinitivement(dossier: Dossier): boolean {
    return dossier.statutElectronique !== StatutDossier.REJETE_DEFINITIF &&
      !STATUTS_DEJA_EXECUTES.has(dossier.statutElectronique);
  }

  basculerRejetDefinitif(dossierId: number): void {
    if (this.rejetDefOuvert.has(dossierId)) {
      this.rejetDefOuvert.delete(dossierId);
    } else {
      this.rejetDefOuvert.add(dossierId);
    }
  }

  motifRejetDefinitif(dossierId: number): string {
    return this.motifsRejetDef.get(dossierId) ?? '';
  }

  modifierMotifRejetDefinitif(dossierId: number, valeur: string): void {
    this.motifsRejetDef.set(dossierId, valeur);
  }

  rejeterDefinitivement(dossier: Dossier): void {
    if (this.rejetDefEnCours.has(dossier.dossierId)) return;
    const motif = this.motifRejetDefinitif(dossier.dossierId).trim();
    if (!motif) return;

    this.rejetDefEnCours.add(dossier.dossierId);
    this.workflowApi.rejeterDefinitif(dossier.dossierId, motif).subscribe({
      next: reponse => {
        dossier.statutElectronique = reponse.statutApres;
        this.rejetDefEnCours.delete(dossier.dossierId);
        this.rejetDefOuvert.delete(dossier.dossierId);
        this.cdr.detectChanges();
      },
      error: () => {
        this.rejetDefEnCours.delete(dossier.dossierId);
        this.cdr.detectChanges();
      }
    });
  }
}