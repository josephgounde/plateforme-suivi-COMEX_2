// Dashboard Admin DSIRI : vue transversale + raccourci vers /admin/parametrage.
// "Vue d'ensemble"/"Tous les dossiers" sont des onglets locaux (?vue=), pas des routes séparées.

import { Component, OnInit, ChangeDetectorRef, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { ReportingApiService } from '../../../../core/api/reporting-api.service';
import { DossierApiService } from '../../../../core/api/dossier-api.service';
import { UtilisateurApiService } from '../../../../core/api/utilisateur-api.service';
import { WorkflowApiService } from '../../../../core/api/workflow-api.service';
import { DashboardData, Dossier, DossierRetard } from '../../../../core/models/dossier.model';
import {
  StatutDossier,
  STATUT_LABELS,
  TYPE_OPERATION_LABELS,
  ProfilUtilisateur
} from '../../../../core/models/enums.model';
import { STATUT_VERS_INDEX_ETAPE } from '../../../../shared/components/workflow-stepper/workflow-stepper.component';
import { RepartitionChartComponent } from '../../../../shared/components/repartition-chart/repartition-chart.component';
import { MetriquesBandeauComponent, MetriqueItem } from '../../../../shared/components/metriques-bandeau/metriques-bandeau.component';
import { GaugeChartComponent } from '../../../../shared/components/gauge-chart/gauge-chart.component';
import { BarChartComponent, BarChartItem } from '../../../../shared/components/bar-chart/bar-chart.component';
import { declencherTelechargement } from '../../../../shared/utils/telechargement.util';
import { DropdownSelectComponent, DropdownOption } from '../../../../shared/components/dropdown-select/dropdown-select.component';
import { PagerComponent } from '../../../../shared/components/pager/pager.component';

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

export type AdminVue = 'apercu' | 'dossiers';

interface EtatReattribution {
  enCours: boolean;
  erreur: string;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, RepartitionChartComponent, MetriquesBandeauComponent, GaugeChartComponent, BarChartComponent, DropdownSelectComponent, PagerComponent],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss'
})
export class AdminDashboardComponent implements OnInit {
  chargement = true;
  dashboard: DashboardData | null = null;
  tousLesDossiers: Dossier[] = [];
  dossiersEnRetard: DossierRetard[] = [];
  vue: AdminVue = 'apercu';
  exportEnCours = false;

  // ── Exports réglementaires (DGI / Trésor / BCEAO) — même bloc que Direction, partagé via DashboardRouterComponent
  reglementaireDateDebut = this.premierJourDuMois();
  reglementaireDateFin = this.aujourdHui();
  exportCrpiDgiEnCours = false;
  exportCrpiTresorEnCours = false;
  exportSituationBceaoEnCours = false;

  readonly statutLabels = STATUT_LABELS;
  readonly typeLabels = TYPE_OPERATION_LABELS;
  gestionnairesConnus: string[] = [];

  get gestionnaireOptions(): DropdownOption[] {
    return this.gestionnairesConnus.map(g => ({ value: g, label: g }));
  }

  // Étapes du circuit (vue "technique", 7 paliers) plutôt que les 21 StatutDossier fins qu'utilise Direction —
  // l'Admin DSIRI supervise où les dossiers s'accumulent, pas le détail métier de chaque statut.
  private readonly etapesCircuit: { libelle: string; couleur: string }[] = [
    { libelle: 'Initiation', couleur: '#90a4ae' },
    { libelle: 'Gestionnaire', couleur: '#42a5f5' },
    { libelle: 'Contrôle COMEX', couleur: '#7e57c2' },
    { libelle: 'Trésorerie', couleur: '#26a69a' },
    { libelle: 'Exécution', couleur: '#ffa726' },
    { libelle: 'Apurement', couleur: '#66bb6a' },
    { libelle: 'Archivage', couleur: '#8d6e63' }
  ];

  private etatsReattribution = new Map<number, EtatReattribution>();

  readonly StatutDossier = StatutDossier;

  // ── Alerte anti-fractionnement + rejet définitif ── le contrôle est porté par le COMEX, mais Admin DSIRI garde un droit d'override (comme Direction).
  leverAlerteEnCours = new Set<number>();
  rejetDefOuvert = new Set<number>();
  rejetDefEnCours = new Set<number>();
  private motifsRejetDef = new Map<number, string>();

  constructor(
    private reporting: ReportingApiService,
    private dossierApi: DossierApiService,
    private utilisateurApi: UtilisateurApiService,
    private workflowApi: WorkflowApiService,
    private route: ActivatedRoute,
    private cdr: ChangeDetectorRef,
    private destroyRef: DestroyRef
  ) {}

  ngOnInit(): void {
    // Abonnement (pas juste route.snapshot) : Angular réutilise l'instance du composant quand seule la query string change.
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      this.vue = params.get('vue') === 'dossiers' ? 'dossiers' : 'apercu';
      this.cdr.detectChanges();
    });

    // Options de réattribution issues du véritable annuaire (pas figées) : un Gestionnaire créé/désactivé se reflète sans code.
    this.utilisateurApi.list().subscribe({
      next: liste => {
        this.gestionnairesConnus = liste
          .filter(u => u.profil === ProfilUtilisateur.GESTIONNAIRE && u.estActif)
          .map(u => u.loginAD);
        this.cdr.detectChanges();
      }
    });

    this.reporting.getDashboard().subscribe({
      next: data => {
        this.dashboard = data;
        this.chargement = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.chargement = false;
        this.cdr.detectChanges();
      }
    });

    this.dossierApi.listDossiers({ pageSize: 200 }).subscribe({
      next: reponse => {
        this.tousLesDossiers = reponse.items;
        this.cdr.detectChanges();
      }
    });

    this.reporting.getDossiersEnRetard().subscribe({
      next: reponse => {
        this.dossiersEnRetard = reponse;
        this.cdr.detectChanges();
      }
    });
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

  // Agrège dashboard.parStatut (21 valeurs fines) en comptage par étape (7 paliers) via STATUT_VERS_INDEX_ETAPE.
  // REJETE_DEFINITIF (index -1, hors circuit normal) devient une ligne à part plutôt qu'ignoré.
  get repartitionCircuit(): { libelle: string; couleur: string; count: number; pourcentage: number; rejete?: boolean }[] {
    if (!this.dashboard) return [];

    const compteurs = new Array(this.etapesCircuit.length).fill(0);
    let rejetes = 0;
    for (const [statut, count] of Object.entries(this.dashboard.parStatut)) {
      const idx = STATUT_VERS_INDEX_ETAPE[statut as StatutDossier] ?? 0;
      if (idx >= 0) compteurs[idx] += count;
      else rejetes += count;
    }

    const max = Math.max(1, ...compteurs, rejetes);
    const lignes: { libelle: string; couleur: string; count: number; pourcentage: number; rejete?: boolean }[] =
      this.etapesCircuit.map((e, i) => ({
        ...e,
        count: compteurs[i],
        pourcentage: Math.round((compteurs[i] / max) * 100)
      }));

    if (rejetes > 0) {
      lignes.push({
        libelle: 'Rejetés définitifs',
        couleur: 'var(--pdoe-red)',
        count: rejetes,
        pourcentage: Math.round((rejetes / max) * 100),
        rejete: true
      });
    }

    return lignes;
  }

  readonly pageSize = PAGE_SIZE;
  pageTousDossiers = 1;

  get tousLesDossiersAffiches(): Dossier[] {
    const totalPages = Math.max(1, Math.ceil(this.tousLesDossiers.length / PAGE_SIZE));
    if (this.pageTousDossiers > totalPages) this.pageTousDossiers = totalPages;
    const debut = (this.pageTousDossiers - 1) * PAGE_SIZE;
    return this.tousLesDossiers.slice(debut, debut + PAGE_SIZE);
  }

  exporterDossiersEnRetard(): void {
    if (this.exportEnCours) return;

    this.exportEnCours = true;
    this.reporting.exportDossiersEnRetard().subscribe({
      next: blob => {
        const date = new Date().toISOString().slice(0, 10);
        declencherTelechargement(blob, `dossiers-en-retard_${date}.xlsx`);
        this.exportEnCours = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.exportEnCours = false;
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

  // Réattribution autorisée tant que le dossier n'a pas dépassé l'étape Gestionnaire (index <= 1) ; confort d'affichage seulement.
  peutReattribuer(dossier: Dossier): boolean {
    return (STATUT_VERS_INDEX_ETAPE[dossier.statutElectronique] ?? 0) <= 1;
  }

  etatReattribution(dossierId: number): EtatReattribution {
    return this.etatsReattribution.get(dossierId) ?? { enCours: false, erreur: '' };
  }

  reattribuer(dossier: Dossier, nouveauGestionnaire: string): void {
    if (!nouveauGestionnaire || nouveauGestionnaire === dossier.gestionnaireAssigne) return;

    this.etatsReattribution.set(dossier.dossierId, { enCours: true, erreur: '' });

    this.dossierApi.reassignerGestionnaire(dossier.dossierId, { gestionnaireLogin: nouveauGestionnaire }).subscribe({
      next: dossierMaj => {
        dossier.gestionnaireAssigne = dossierMaj.gestionnaireAssigne;
        this.etatsReattribution.set(dossier.dossierId, { enCours: false, erreur: '' });
        this.cdr.detectChanges();
      },
      error: () => {
        this.etatsReattribution.set(dossier.dossierId, {
          enCours: false,
          erreur: 'Échec — ce dossier a peut-être déjà dépassé l\'étape Gestionnaire.'
        });
        this.cdr.detectChanges();
      }
    });
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