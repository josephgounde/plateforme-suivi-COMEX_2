// Dashboard Agent d'accueil.

import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { DossierApiService } from '../../../../core/api/dossier-api.service';
import { ReportingApiService } from '../../../../core/api/reporting-api.service';
import { Dossier, DashboardData } from '../../../../core/models/dossier.model';
import {
  StatutDossier,
  TypeOperation,
  STATUT_LABELS,
  TYPE_OPERATION_LABELS
} from '../../../../core/models/enums.model';
import { HttpErrorResponse } from '@angular/common/http';
import { DashboardNavService } from '../../../../core/layout/dashboard-nav.service';
import { ToastService } from '../../../../core/toast/toast.service';
import { MetriquesBandeauComponent, MetriqueItem } from '../../../../shared/components/metriques-bandeau/metriques-bandeau.component';
import { RepartitionChartComponent, RepartitionItem } from '../../../../shared/components/repartition-chart/repartition-chart.component';
import { couleurStatutBadge } from '../../../../shared/utils/statut-couleur.util';

// Statuts "validés" côté Agent d'accueil : à partir de l'exécution réussie.
const STATUTS_VALIDES: ReadonlySet<StatutDossier> = new Set([
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
import { NotificationPanelComponent } from '../../../../shared/components/notification-panel/notification-panel.component';
import { DropdownSelectComponent, DropdownOption } from '../../../../shared/components/dropdown-select/dropdown-select.component';
import { NotificationsService } from '../../../../core/notifications/notifications.service';
import { PagerComponent } from '../../../../shared/components/pager/pager.component';

const PAGE_SIZE = 12;

// Un module = un onglet de la barre latérale qui remplace le contenu affiché, pas un ancrage de défilement.
export type VueAgentAccueil = 'brouillons' | 'rejetes' | 'en-cours' | 'valides' | 'statistiques';

interface ModuleNav {
  vue: VueAgentAccueil;
  icone: string;
  libelle: string;
  count: number;
}

@Component({
  selector: 'app-agent-accueil-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    NotificationPanelComponent,
    DropdownSelectComponent,
    PagerComponent,
    MetriquesBandeauComponent,
    RepartitionChartComponent
  ],
  templateUrl: './agent-accueil-dashboard.component.html',
  styleUrl: './agent-accueil-dashboard.component.scss'
})
export class AgentAccueilDashboardComponent implements OnInit, OnDestroy {
  chargement = true;
  tousLesDossiers: Dossier[] = [];
  soumissionEnCours = new Set<number>();
  soumissionErreurs = new Map<number, string>();

  // Onglet "Mes statistiques" — même DashboardResponse que les dashboards Direction/Admin/COMEX, mais
  // scopé côté backend aux dossiers créés par l'agent courant (GetMesStatistiquesHandler).
  mesStats: DashboardData | null = null;

  // Filtres (recherche/type/pays/tri) appliqués par-dessus la section active, pas des vues supplémentaires.
  // Recherche à validation explicite (bouton/Entrée) : rechercheSaisie = valeur du champ, recherche = valeur appliquée.
  rechercheSaisie = '';
  recherche = '';
  filtreType: TypeOperation | 'TOUS' = 'TOUS';
  filtrePays: string | 'TOUS' = 'TOUS';
  tri: 'recent' | 'montant' = 'recent';
  vueGrille = false;
  afficherFiltresAvances = false;
  afficherMontant = true;

  readonly statutLabels = STATUT_LABELS;
  readonly typeLabels = TYPE_OPERATION_LABELS;
  readonly StatutDossier = StatutDossier;
  readonly TypeOperation = TypeOperation;
  readonly typesOperation = Object.values(TypeOperation);

  constructor(
    private dossierApi: DossierApiService,
    private reporting: ReportingApiService,
    private cdr: ChangeDetectorRef,
    public notifs: NotificationsService,
    private dashboardNav: DashboardNavService,
    private toast: ToastService
  ) {}

  // Onglet actif — porté par DashboardNavService, pilotée par la barre latérale globale. 'brouillons' par défaut avant ngOnInit.
  get vueActive(): VueAgentAccueil {
    return (this.dashboardNav.activeId() as VueAgentAccueil | null) ?? 'brouillons';
  }

  ngOnInit(): void {
    this.notifs.charger();
    this.dashboardNav.select('brouillons');
    this.dashboardNav.setItems(this.versNavItems());

    this.dossierApi.listDossiers({ pageSize: 200 }).subscribe({
      next: reponse => {
        this.tousLesDossiers = reponse.items;
        this.chargement = false;
        this.dashboardNav.setItems(this.versNavItems());
        this.cdr.detectChanges();
      },
      error: () => {
        this.chargement = false;
        this.cdr.detectChanges();
      }
    });

    this.reporting.getMesStatistiques().subscribe({
      next: data => {
        this.mesStats = data;
        this.cdr.detectChanges();
      }
    });
  }

  // Sans ce clear, la barre latérale continuerait d'afficher les onglets Brouillons/Rejetés/... d'une page qui n'est plus affichée.
  ngOnDestroy(): void {
    this.dashboardNav.clear();
  }

  private versNavItems() {
    return this.modulesNav.map(m => ({ id: m.vue, icone: m.icone, libelle: m.libelle, count: m.count }));
  }

  // Brouillons neufs, jamais soumis — à distinguer des brouillons revenus d'un rejet (même statut, contexte différent).
  get brouillons(): Dossier[] {
    return this.tousLesDossiers.filter(
      d => d.statutElectronique === StatutDossier.BROUILLON && !d.estRejeteVersAgentAccueil
    );
  }

  // Brouillons renvoyés par rejet (correctibles) + rejets définitifs Direction (lecture seule, aucune correction possible).
  get dossiersRejetes(): Dossier[] {
    return this.tousLesDossiers.filter(
      d =>
        (d.statutElectronique === StatutDossier.BROUILLON && d.estRejeteVersAgentAccueil) ||
        d.statutElectronique === StatutDossier.REJETE_DEFINITIF
    );
  }

  // Transfert exécuté avec succès — cf. STATUTS_VALIDES.
  get dossiersValides(): Dossier[] {
    return this.tousLesDossiers.filter(d => STATUTS_VALIDES.has(d.statutElectronique));
  }

  // Tout le reste : soumis, ni rejeté ni encore exécuté.
  get dossiersEnCours(): Dossier[] {
    return this.tousLesDossiers.filter(
      d =>
        d.statutElectronique !== StatutDossier.BROUILLON &&
        d.statutElectronique !== StatutDossier.REJETE_DEFINITIF &&
        !STATUTS_VALIDES.has(d.statutElectronique)
    );
  }

  // Toujours les 5 modules, même à 0 : ce sont de vrais onglets de navigation, pas des ancres de défilement.
  get modulesNav(): ModuleNav[] {
    return [
      { vue: 'brouillons', icone: '📝', libelle: 'Brouillons', count: this.brouillons.length },
      { vue: 'rejetes', icone: '⚠', libelle: 'Rejetés', count: this.dossiersRejetes.length },
      { vue: 'en-cours', icone: '⏳', libelle: 'En cours', count: this.dossiersEnCours.length },
      { vue: 'valides', icone: '✓', libelle: 'Validés', count: this.dossiersValides.length },
      { vue: 'statistiques', icone: '📊', libelle: 'Mes statistiques', count: this.mesStats?.totalDossiers ?? 0 }
    ];
  }

  get libelleSectionActive(): string {
    return this.modulesNav.find(m => m.vue === this.vueActive)?.libelle ?? '';
  }

  // Dossiers de l'onglet actif, avant recherche/filtres/tri — délègue aux getters existants pour la logique métier.
  // 'statistiques' n'affiche pas de liste de dossiers (cf. metriquesMesStats/repartitionMesStats) — [] par défaut.
  private get dossiersSection(): Dossier[] {
    switch (this.vueActive) {
      case 'brouillons': return this.brouillons;
      case 'rejetes': return this.dossiersRejetes;
      case 'en-cours': return this.dossiersEnCours;
      case 'valides': return this.dossiersValides;
      case 'statistiques': return [];
    }
  }

  // ── Mes statistiques ── mêmes composants partagés (metriques-bandeau / repartition-chart) que Direction/Admin/COMEX.
  get metriquesMesStats(): MetriqueItem[] {
    if (!this.mesStats) return [];
    const items: MetriqueItem[] = [
      { libelle: 'Dossiers créés', valeur: String(this.mesStats.totalDossiers) }
    ];
    if (this.mesStats.dossiersEnRetard > 0) {
      items.push({ libelle: 'En retard', valeur: String(this.mesStats.dossiersEnRetard), accent: 'danger' });
    }
    if (this.mesStats.dossiersApurementProche > 0) {
      items.push({ libelle: 'Échéance < 30 j', valeur: String(this.mesStats.dossiersApurementProche), accent: 'warning' });
    }
    items.push({ libelle: "Taux d'apurement", valeur: `${Math.round(this.mesStats.tauxApurement * 100)} %`, accent: 'success' });
    return items;
  }

  get repartitionMesStats(): RepartitionItem[] {
    if (!this.mesStats) return [];
    const entrees = Object.entries(this.mesStats.parStatut)
      .filter(([, count]) => count > 0)
      .sort((a, b) => b[1] - a[1]);
    const max = Math.max(1, ...entrees.map(([, count]) => count));
    return entrees.map(([statut, count]) => ({
      libelle: this.statutLabels[statut as StatutDossier],
      couleur: couleurStatutBadge(statut as StatutDossier),
      count,
      pourcentage: Math.round((count / max) * 100)
    }));
  }

  // Pays bénéficiaires réellement présents dans la section active — pas une liste figée qui proposerait des pays absents.
  get paysDisponibles(): string[] {
    return [...new Set(this.dossiersSection.map(d => d.paysBeneficiaire))].sort();
  }

  get paysOptions(): DropdownOption[] {
    return [
      { value: 'TOUS', label: 'Tous pays' },
      ...this.paysDisponibles.map(pays => ({ value: pays, label: pays }))
    ];
  }

  readonly triOptions: DropdownOption[] = [
    { value: 'recent', label: 'Plus récent' },
    { value: 'montant', label: 'Montant décroissant' }
  ];

  appliquerRecherche(): void {
    this.recherche = this.rechercheSaisie.trim();
  }

  effacerRecherche(): void {
    this.rechercheSaisie = '';
    this.recherche = '';
  }

  // Couche recherche + type + pays + tri, appliquée par-dessus la
  // section active — c'est ce que le template affiche réellement.
  get dossiersAffiches(): Dossier[] {
    let resultat = this.dossiersSection;

    if (this.filtreType !== 'TOUS') {
      resultat = resultat.filter(d => d.typeOperation === this.filtreType);
    }

    if (this.filtrePays !== 'TOUS') {
      resultat = resultat.filter(d => d.paysBeneficiaire === this.filtrePays);
    }

    const terme = this.recherche.trim().toLowerCase();
    if (terme) {
      resultat = resultat.filter(
        d =>
          d.referenceInterne.toLowerCase().includes(terme) ||
          d.nomClient.toLowerCase().includes(terme)
      );
    }

    return [...resultat].sort((a, b) =>
      this.tri === 'montant'
        ? b.montant - a.montant
        : new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()
    );
  }

  readonly pageSize = PAGE_SIZE;
  page = 1;

  get dossiersAffichesPage(): Dossier[] {
    const liste = this.dossiersAffiches;
    const totalPages = Math.max(1, Math.ceil(liste.length / PAGE_SIZE));
    if (this.page > totalPages) this.page = totalPages;
    const debut = (this.page - 1) * PAGE_SIZE;
    return liste.slice(debut, debut + PAGE_SIZE);
  }

  // Déclenche BROUILLON → EN_VALIDATION_GESTIONNAIRE. Mutation en place : la ligne bascule de section automatiquement.
  soumettre(dossier: Dossier): void {
    if (this.soumissionEnCours.has(dossier.dossierId)) return;

    this.soumissionEnCours.add(dossier.dossierId);
    this.soumissionErreurs.delete(dossier.dossierId);
    this.dossierApi.soumettreDossier(dossier.dossierId).subscribe({
      next: dossierMaj => {
        dossier.statutElectronique = dossierMaj.statutElectronique;
        this.soumissionEnCours.delete(dossier.dossierId);
        this.dashboardNav.setItems(this.versNavItems());
        this.cdr.detectChanges();
      },
      error: (erreur: HttpErrorResponse) => {
        this.soumissionEnCours.delete(dossier.dossierId);
        const message = erreur.error?.message ?? 'Échec de la soumission — réessayez.';
        this.soumissionErreurs.set(dossier.dossierId, message);
        this.toast.erreur(`${dossier.referenceInterne} — ${message}`);
        this.cdr.detectChanges();
      }
    });
  }
}