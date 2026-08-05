// Dashboard Trésorerie : dossiers en EN_AVIS_TRESORERIE. Taux de change, disponibilité des fonds, correspondant
// et date de débit en un seul PATCH /dossiers/{id}/tresorerie, puis validation depuis le détail.

import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { DossierApiService } from '../../../../core/api/dossier-api.service';
import { WorkflowApiService } from '../../../../core/api/workflow-api.service';
import { ParametrageApiService } from '../../../../core/api/parametrage-api.service';
import { ReportingApiService } from '../../../../core/api/reporting-api.service';
import { Dossier, TresorerieUpdateRequest, DashboardData } from '../../../../core/models/dossier.model';
import { StatutDossier, NiveauValidation, STATUT_LABELS, TYPE_OPERATION_LABELS } from '../../../../core/models/enums.model';
import { MockWorkflowConfigStore } from '../../../../core/mock/mock-workflow-config.store';
import { DelaiTraitementComponent } from '../../../../shared/components/delai-traitement/delai-traitement.component';
import { RepartitionChartComponent, RepartitionItem } from '../../../../shared/components/repartition-chart/repartition-chart.component';
import { MetriquesBandeauComponent, MetriqueItem } from '../../../../shared/components/metriques-bandeau/metriques-bandeau.component';
import { NotificationPanelComponent } from '../../../../shared/components/notification-panel/notification-panel.component';
import { DropdownSelectComponent, DropdownOption } from '../../../../shared/components/dropdown-select/dropdown-select.component';
import { NotificationsService } from '../../../../core/notifications/notifications.service';
import { DashboardNavService } from '../../../../core/layout/dashboard-nav.service';
import { PagerComponent } from '../../../../shared/components/pager/pager.component';
import { couleurStatutBadge } from '../../../../shared/utils/statut-couleur.util';

const PAGE_SIZE = 8;

interface EtatLigneTresorerie {
  ouvert: boolean;
  enregistrement: boolean;
  erreur: boolean;
  enregistre: boolean;
}

// Périmètre affiché directement en file d'attente — le reste (déjà transmis ou rejeté) va dans "Historique".
const STATUTS_FILE_ATTENTE: ReadonlySet<StatutDossier> = new Set([
  StatutDossier.EN_AVIS_TRESORERIE,
  StatutDossier.AVIS_TRESORERIE_DONNE
]);

// Un module = un onglet de la barre latérale qui remplace le contenu affiché, pas un ancrage de défilement.
type VueTresorerie = 'file-attente' | 'historique' | 'statistiques';

interface ModuleNav {
  vue: VueTresorerie;
  icone: string;
  libelle: string;
  count: number;
}

@Component({
  selector: 'app-tresorerie-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    DelaiTraitementComponent,
    RepartitionChartComponent,
    MetriquesBandeauComponent,
    NotificationPanelComponent,
    DropdownSelectComponent,
    PagerComponent
  ],
  templateUrl: './tresorerie-dashboard.component.html',
  styleUrl: './tresorerie-dashboard.component.scss'
})
export class TresorerieDashboardComponent implements OnInit, OnDestroy {
  chargement = true;
  dossiers: Dossier[] = [];

  // Délai imparti à l'étape Trésorerie (ParametreMetier.
  // DELAI_TRESORERIE_HEURES).
  delaiHeures: number | null = null;

  // Onglet "Mes statistiques" — scopé côté backend aux dossiers où l'agent a personnellement enregistré
  // une étape ETAPE_4_TRESORERIE (pas de champ d'assignation Trésorerie sur Dossier, contrairement au Gestionnaire).
  mesStats: DashboardData | null = null;

  etatsLignes = new Map<number, EtatLigneTresorerie>();
  formulaires = new Map<number, FormGroup>();

  transmissionEnCours = new Set<number>();

  // ── Taux de change ── coté automatiquement à l'ouverture du formulaire ; chargementTaux désactive "Actualiser" pendant l'appel.
  chargementTaux = new Set<number>();
  dateCotation = new Map<number, string>();

  readonly typeLabels = TYPE_OPERATION_LABELS;
  readonly statutLabels = STATUT_LABELS;

  constructor(
    private dossierApi: DossierApiService,
    private workflowApi: WorkflowApiService,
    private fb: FormBuilder,
    private cdr: ChangeDetectorRef,
    private workflowConfig: MockWorkflowConfigStore,
    private parametrageApi: ParametrageApiService,
    private reporting: ReportingApiService,
    private dashboardNav: DashboardNavService,
    public notifs: NotificationsService
  ) {}

  // Onglet actif — porté par DashboardNavService, cf.
  // AgentAccueilDashboardComponent pour le raisonnement complet.
  get vueActive(): VueTresorerie {
    return (this.dashboardNav.activeId() as VueTresorerie | null) ?? 'file-attente';
  }

  // Toujours les 3 modules, même à 0 : ce sont de vrais onglets de navigation, pas des ancres de défilement.
  get modulesNav(): ModuleNav[] {
    return [
      { vue: 'file-attente', icone: '📥', libelle: "File d'attente", count: this.dossiersFileAttente.length },
      { vue: 'historique', icone: '📚', libelle: 'Historique', count: this.dossiersHistorique.length },
      { vue: 'statistiques', icone: '📊', libelle: 'Mes statistiques', count: this.mesStats?.totalDossiers ?? 0 }
    ];
  }

  // ── Mes statistiques ── mêmes composants partagés (metriques-bandeau / repartition-chart) que Direction/Admin/COMEX.
  get metriquesMesStats(): MetriqueItem[] {
    if (!this.mesStats) return [];
    const items: MetriqueItem[] = [
      { libelle: 'Dossiers traités', valeur: String(this.mesStats.totalDossiers) }
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

  ngOnInit(): void {
    this.notifs.charger();
    this.dashboardNav.select('file-attente');
    this.dashboardNav.setItems(this.versNavItems());

    this.parametrageApi.get('DELAI_TRESORERIE_HEURES').subscribe({
      next: p => {
        this.delaiHeures = Number(p.valeur);
        this.cdr.detectChanges();
      }
    });

    this.dossierApi.listDossiers({ pageSize: 200 }).subscribe({
      next: reponse => {
        this.dossiers = reponse.items;
        this.dossiers.forEach(d => {
          this.etatsLignes.set(d.dossierId, {
            ouvert: false,
            enregistrement: false,
            erreur: false,
            enregistre: !!(d.tauxChange && d.correspondantDesigne)
          });
          this.formulaires.set(d.dossierId, this.fb.group({
            tauxChange: [d.tauxChange ?? '', [Validators.required, Validators.min(0.000001)]],
            deviseCotation: [d.deviseCotation ?? 'EUR', Validators.required],
            correspondantDesigne: [d.correspondantDesigne ?? '', Validators.required],
            bicCorrespondant: [d.bicCorrespondant ?? '', Validators.required],
            dateDebit: [d.dateDebit ?? '', Validators.required],
            couverture: [d.couverture ?? ''],
            disponibiliteFonds: [d.disponibiliteFonds ?? false]
          }));
        });
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

  ngOnDestroy(): void {
    this.dashboardNav.clear();
  }

  private versNavItems() {
    return this.modulesNav.map(m => ({ id: m.vue, icone: m.icone, libelle: m.libelle, count: m.count }));
  }

  // File d'attente réelle — seule section actionnable.
  get dossiersFileAttente(): Dossier[] {
    // !etapeGenerique : cf. ComexDashboardComponent.dossiersControle.
    return this.dossiers.filter(d => STATUTS_FILE_ATTENTE.has(d.statutElectronique) && !d.etapeGenerique);
  }

  // Dossiers déjà avisés puis transmis plus loin, ou rejetés définitivement — lecture seule.
  get dossiersHistorique(): Dossier[] {
    return this.dossiers.filter(d => !STATUTS_FILE_ATTENTE.has(d.statutElectronique));
  }

  readonly pageSize = PAGE_SIZE;
  pageFileAttente = 1;
  pageHistorique = 1;

  private paginer(liste: Dossier[], page: 'pageFileAttente' | 'pageHistorique'): Dossier[] {
    const totalPages = Math.max(1, Math.ceil(liste.length / PAGE_SIZE));
    if (this[page] > totalPages) this[page] = totalPages;
    const debut = (this[page] - 1) * PAGE_SIZE;
    return liste.slice(debut, debut + PAGE_SIZE);
  }

  get dossiersFileAttenteAffiches(): Dossier[] {
    return this.paginer(this.dossiersFileAttente, 'pageFileAttente');
  }

  get dossiersHistoriqueAffiches(): Dossier[] {
    return this.paginer(this.dossiersHistorique, 'pageHistorique');
  }

  // Exposition devise de la file d'attente — jamais visible ailleurs, utile d'un coup d'œil avant de traiter la file.
  private readonly paletteDevises = ['#42a5f5', '#7e57c2', '#26a69a', '#ffa726', '#66bb6a', '#8d6e63', '#ec407a'];

  get repartitionDevises(): RepartitionItem[] {
    const compteurs = new Map<string, number>();
    for (const d of this.dossiersFileAttente) {
      compteurs.set(d.devise, (compteurs.get(d.devise) ?? 0) + 1);
    }
    const entrees = [...compteurs.entries()].sort((a, b) => b[1] - a[1]);
    const max = Math.max(1, ...entrees.map(([, count]) => count));
    return entrees.map(([devise, count], i) => ({
      libelle: devise,
      couleur: this.paletteDevises[i % this.paletteDevises.length],
      count,
      pourcentage: Math.round((count / max) * 100)
    }));
  }

  heureCotation(dossierId: number): string {
    const iso = this.dateCotation.get(dossierId);
    if (!iso) return '';
    return new Date(iso).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' });
  }

  etat(dossierId: number): EtatLigneTresorerie {
    return this.etatsLignes.get(dossierId) ?? {
      ouvert: false, enregistrement: false, erreur: false, enregistre: false
    };
  }

  formulaire(dossierId: number): FormGroup {
    return this.formulaires.get(dossierId)!;
  }

  basculerFormulaire(dossierId: number): void {
    const e = this.etat(dossierId);
    const ouverture = !e.ouvert;
    this.etatsLignes.set(dossierId, { ...e, ouvert: ouverture, erreur: false });

    // Cotation automatique à la première ouverture seulement — si un taux existe déjà, on ne l'écrase pas.
    const tauxActuel = this.formulaire(dossierId).get('tauxChange')?.value;
    if (ouverture && !e.enregistre && !tauxActuel) {
      this.actualiserTaux(dossierId);
    }
  }

  // versDevise force la devise de cotation (utile juste après un changement de menu, avant que le FormControl soit à jour).
  actualiserTaux(dossierId: number, versDevise?: string): void {
    if (this.chargementTaux.has(dossierId)) return;
    const dossier = this.dossiers.find(d => d.dossierId === dossierId);
    if (!dossier) return;

    const cible = versDevise || this.formulaire(dossierId).get('deviseCotation')?.value || 'XOF';

    this.chargementTaux.add(dossierId);
    this.dossierApi.obtenirTauxChange(dossier.devise, cible).subscribe({
      next: resultat => {
        this.formulaire(dossierId).patchValue({
          tauxChange: resultat.taux,
          deviseCotation: resultat.deviseCotation
        });
        this.dateCotation.set(dossierId, resultat.dateCotation);
        this.chargementTaux.delete(dossierId);
        this.cdr.detectChanges();
      },
      error: () => {
        this.chargementTaux.delete(dossierId);
        this.cdr.detectChanges();
      }
    });
  }

  // ── Devise de cotation : menu déroulant + saisie libre ── changer la devise redemande un taux (l'ancien serait faux pour la nouvelle paire).
  readonly devisesCourantes = ['XOF', 'EUR', 'USD', 'GBP', 'CHF', 'CAD', 'CNY'];

  // Options du menu déroulant — dérivées de devisesCourantes, "Autre…" ajouté à la fin.
  readonly deviseOptions: DropdownOption[] = [
    ...this.devisesCourantes.map(d => ({ value: d, label: d })),
    { value: 'AUTRE', label: 'Autre…' }
  ];

  // Dérivé du FormControl plutôt que d'un état séparé : pas de double source de vérité.
  deviseCotationSelection(dossierId: number): string {
    const valeur = (this.formulaire(dossierId).get('deviseCotation')?.value ?? '').toUpperCase();
    return this.devisesCourantes.includes(valeur) ? valeur : 'AUTRE';
  }

  choisirDeviseCotation(dossierId: number, valeur: string): void {
    if (valeur === 'AUTRE') {
      // Vide le champ pour forcer une vraie saisie (sinon un ancien code connu masquerait le champ texte).
      this.formulaire(dossierId).get('deviseCotation')?.setValue('');
      return;
    }
    this.formulaire(dossierId).get('deviseCotation')?.setValue(valeur);
    this.actualiserTaux(dossierId, valeur);
  }

  enregistrer(dossierId: number): void {
    const form = this.formulaire(dossierId);
    if (form.invalid) return;

    const e = this.etat(dossierId);
    this.etatsLignes.set(dossierId, { ...e, enregistrement: true, erreur: false });

    const v = form.value;
    const req: TresorerieUpdateRequest = {
      tauxChange: Number(v.tauxChange),
      deviseCotation: v.deviseCotation,
      correspondantDesigne: v.correspondantDesigne,
      bicCorrespondant: v.bicCorrespondant,
      dateDebit: v.dateDebit,
      couverture: v.couverture || undefined,
      disponibiliteFonds: !!v.disponibiliteFonds
    };

    this.dossierApi.updateTresorerie(dossierId, req).subscribe({
      next: dossierMaj => {
        const idx = this.dossiers.findIndex(d => d.dossierId === dossierId);
        if (idx >= 0) this.dossiers[idx] = dossierMaj;
        this.etatsLignes.set(dossierId, {
          ouvert: false,
          enregistrement: false,
          erreur: false,
          enregistre: true
        });
        this.cdr.detectChanges();
      },
      error: () => {
        this.etatsLignes.set(dossierId, { ...e, enregistrement: false, erreur: true });
        this.cdr.detectChanges();
      }
    });
  }

  // N'est proposé qu'une fois les paramètres Trésorerie enregistrés
  // (etat.enregistre).
  peutValiderEtTransmettre(dossierId: number): boolean {
    return this.etat(dossierId).enregistre;
  }

  validerEtTransmettre(dossier: Dossier): void {
    if (this.transmissionEnCours.has(dossier.dossierId)) return;
    if (!this.peutValiderEtTransmettre(dossier.dossierId)) return;

    this.transmissionEnCours.add(dossier.dossierId);
    this.workflowApi
      .valider(dossier.dossierId, { niveauValidation: NiveauValidation.ETAPE_4_TRESORERIE })
      .subscribe({
        next: reponse => {
          dossier.statutElectronique = reponse.statutApres;
          this.transmissionEnCours.delete(dossier.dossierId);
          this.dashboardNav.setItems(this.versNavItems());
          this.cdr.detectChanges();
        },
        error: () => {
          this.transmissionEnCours.delete(dossier.dossierId);
          this.cdr.detectChanges();
        }
      });
  }

  // ── Rejet pour correction ── trois cibles possibles : Agent d'accueil, Gestionnaire ou COMEX.
  // Calculé depuis MockWorkflowConfigStore — cf. ComexDashboardComponent (évite de proposer une étape désactivée).
  get ciblesRejetPossibles(): { valeur: string; libelle: string }[] {
    const codesConnus = new Set([
      NiveauValidation.ETAPE_1_INITIATION as string,
      NiveauValidation.ETAPE_2_GESTIONNAIRE as string,
      NiveauValidation.ETAPE_3_COMEX as string
    ]);
    return this.workflowConfig.etapesActives
      .filter(e => codesConnus.has(e.code))
      .map(e => ({ valeur: e.code, libelle: e.libelle }));
  }

  get ciblesRejetOptions(): DropdownOption[] {
    return this.ciblesRejetPossibles.map(c => ({ value: c.valeur, label: c.libelle }));
  }

  rejetOuvert = new Set<number>();
  rejetEnCours = new Set<number>();
  private motifsRejet = new Map<number, string>();
  private ciblesRejet = new Map<number, string>();

  basculerRejet(dossierId: number): void {
    if (this.rejetOuvert.has(dossierId)) {
      this.rejetOuvert.delete(dossierId);
    } else {
      this.rejetOuvert.add(dossierId);
      if (!this.ciblesRejet.has(dossierId)) {
        this.ciblesRejet.set(dossierId, NiveauValidation.ETAPE_1_INITIATION);
      }
    }
  }

  motifRejet(dossierId: number): string {
    return this.motifsRejet.get(dossierId) ?? '';
  }

  modifierMotifRejet(dossierId: number, valeur: string): void {
    this.motifsRejet.set(dossierId, valeur);
  }

  cibleRejet(dossierId: number): string {
    return this.ciblesRejet.get(dossierId) ?? NiveauValidation.ETAPE_1_INITIATION;
  }

  modifierCibleRejet(dossierId: number, valeur: string): void {
    this.ciblesRejet.set(dossierId, valeur);
  }

  peutRejeter(dossier: Dossier): boolean {
    return this.motifRejet(dossier.dossierId).trim().length > 0;
  }

  rejeter(dossier: Dossier): void {
    if (this.rejetEnCours.has(dossier.dossierId)) return;
    if (!this.peutRejeter(dossier)) return;

    this.rejetEnCours.add(dossier.dossierId);
    this.workflowApi
      .rejeter(dossier.dossierId, {
        niveauValidation: NiveauValidation.ETAPE_4_TRESORERIE,
        motifRejet: this.motifRejet(dossier.dossierId),
        responsableCorrection: this.cibleRejet(dossier.dossierId)
      })
      .subscribe({
        next: reponse => {
          dossier.statutElectronique = reponse.statutApres;
          this.rejetEnCours.delete(dossier.dossierId);
          this.rejetOuvert.delete(dossier.dossierId);
          this.dashboardNav.setItems(this.versNavItems());
          this.cdr.detectChanges();
        },
        error: () => {
          this.rejetEnCours.delete(dossier.dossierId);
          this.cdr.detectChanges();
        }
      });
  }
}