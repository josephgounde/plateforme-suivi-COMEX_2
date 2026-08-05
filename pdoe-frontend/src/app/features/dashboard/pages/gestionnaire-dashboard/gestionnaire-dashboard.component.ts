// Dashboard Gestionnaire : file d'attente (délai 4h) triée par plus ancien d'abord, plus un historique en lecture seule.
// La confirmation de commande et la transmission vers COMEX vivent sur la page détail.

import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DossierApiService } from '../../../../core/api/dossier-api.service';
import { WorkflowApiService } from '../../../../core/api/workflow-api.service';
import { ParametrageApiService } from '../../../../core/api/parametrage-api.service';
import { ReportingApiService } from '../../../../core/api/reporting-api.service';
import { Dossier, DashboardData } from '../../../../core/models/dossier.model';
import { StatutDossier, NiveauValidation, CanalNotification, STATUT_LABELS, TYPE_OPERATION_LABELS } from '../../../../core/models/enums.model';
import { DelaiTraitementComponent } from '../../../../shared/components/delai-traitement/delai-traitement.component';
import { NotificationPanelComponent } from '../../../../shared/components/notification-panel/notification-panel.component';
import { NotificationsService } from '../../../../core/notifications/notifications.service';
import { DashboardNavService } from '../../../../core/layout/dashboard-nav.service';
import { DropdownSelectComponent, DropdownOption } from '../../../../shared/components/dropdown-select/dropdown-select.component';
import { PagerComponent } from '../../../../shared/components/pager/pager.component';
import { MetriquesBandeauComponent, MetriqueItem } from '../../../../shared/components/metriques-bandeau/metriques-bandeau.component';
import { RepartitionChartComponent, RepartitionItem } from '../../../../shared/components/repartition-chart/repartition-chart.component';
import { couleurStatutBadge } from '../../../../shared/utils/statut-couleur.util';

const PAGE_SIZE = 8;

// ANTI_FRACTIONNEMENT_DETECTE n'y figure plus : ce contrôle est désormais porté par le COMEX, pas le Gestionnaire.
const STATUTS_FILE_ATTENTE: ReadonlySet<StatutDossier> = new Set([
  StatutDossier.EN_VALIDATION_GESTIONNAIRE
]);

interface EtatNotifClient {
  ouvert: boolean;
  canal: CanalNotification;
  message: string;
  enCours: boolean;
  succes: string | null; // destinataire (email/téléphone) une fois envoyé
  erreur: boolean;
}

// Un module = un onglet de la barre latérale qui remplace le contenu affiché, pas un ancrage de défilement.
type VueGestionnaire = 'file-attente' | 'historique' | 'statistiques';

interface ModuleNav {
  vue: VueGestionnaire;
  icone: string;
  libelle: string;
  count: number;
}

@Component({
  selector: 'app-gestionnaire-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    DelaiTraitementComponent,
    NotificationPanelComponent,
    DropdownSelectComponent,
    PagerComponent,
    MetriquesBandeauComponent,
    RepartitionChartComponent
  ],
  templateUrl: './gestionnaire-dashboard.component.html',
  styleUrl: './gestionnaire-dashboard.component.scss'
})
export class GestionnaireDashboardComponent implements OnInit, OnDestroy {
  chargement = true;
  dossiers: Dossier[] = [];

  // Délai de l'étape Gestionnaire (ParametreMetier.DELAI_GESTIONNAIRE_HEURES), consommé par <app-delai-traitement>.
  delaiHeures: number | null = null;

  // Onglet "Mes statistiques" — scopé côté backend aux dossiers assignés au gestionnaire courant (GestionnaireAssigneLogin).
  mesStats: DashboardData | null = null;

  readonly typeLabels = TYPE_OPERATION_LABELS;
  readonly statutLabels = STATUT_LABELS;

  readonly canalOptions: DropdownOption[] = [
    { value: CanalNotification.EMAIL, label: 'Email' },
    { value: CanalNotification.SMS, label: 'SMS' },
    { value: CanalNotification.SMS_ET_EMAIL, label: 'SMS et e-mail' }
  ];

  constructor(
    private dossierApi: DossierApiService,
    private workflowApi: WorkflowApiService,
    private parametrageApi: ParametrageApiService,
    private reporting: ReportingApiService,
    private cdr: ChangeDetectorRef,
    public notifs: NotificationsService,
    private dashboardNav: DashboardNavService
  ) {}

  // Onglet actif — porté par DashboardNavService, cf.
  // AgentAccueilDashboardComponent pour le raisonnement complet.
  get vueActive(): VueGestionnaire {
    return (this.dashboardNav.activeId() as VueGestionnaire | null) ?? 'file-attente';
  }

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
      { libelle: 'Dossiers assignés', valeur: String(this.mesStats.totalDossiers) }
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

    this.parametrageApi.get('DELAI_GESTIONNAIRE_HEURES').subscribe({
      next: p => {
        this.delaiHeures = Number(p.valeur);
        this.cdr.detectChanges();
      }
    });

    this.dossierApi.listDossiers({ pageSize: 200 }).subscribe({
      next: reponse => {
        this.dossiers = [...reponse.items].sort(
          (a, b) => new Date(a.updatedAt).getTime() - new Date(b.updatedAt).getTime()
        );

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

  // File d'attente réelle — la confirmation de commande et la transmission se font depuis la page de détail (Examiner).
  get dossiersFileAttente(): Dossier[] {
    // !etapeGenerique : cf. ComexDashboardComponent.dossiersControle.
    return this.dossiers.filter(d => STATUTS_FILE_ATTENTE.has(d.statutElectronique) && !d.etapeGenerique);
  }

  // Dossiers déjà examinés mais transmis plus loin ou rejetés définitivement — plus récent en premier (inverse de la file).
  get dossiersHistorique(): Dossier[] {
    return this.dossiers
      .filter(d => !STATUTS_FILE_ATTENTE.has(d.statutElectronique))
      .slice()
      .reverse();
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

  // ── Mise en attente pour correction ── seule cible possible : l'Agent d'accueil (ETAPE_1), rien d'autre en amont.
  rejetOuvert = new Set<number>();
  rejetEnCours = new Set<number>();
  private motifsRejet = new Map<number, string>();

  basculerRejet(dossierId: number): void {
    if (this.rejetOuvert.has(dossierId)) {
      this.rejetOuvert.delete(dossierId);
    } else {
      this.rejetOuvert.add(dossierId);
    }
  }

  motifRejet(dossierId: number): string {
    return this.motifsRejet.get(dossierId) ?? '';
  }

  modifierMotifRejet(dossierId: number, valeur: string): void {
    this.motifsRejet.set(dossierId, valeur);
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
        niveauValidation: NiveauValidation.ETAPE_2_GESTIONNAIRE,
        motifRejet: this.motifRejet(dossier.dossierId),
        responsableCorrection: NiveauValidation.ETAPE_1_INITIATION
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

  // ── Notifier le client (SMS/Email) ── action indépendante du rejet ci-dessus, avec son propre canal/message.
  private etatsNotifClient = new Map<number, EtatNotifClient>();

  etatNotifClient(dossierId: number): EtatNotifClient {
    return (
      this.etatsNotifClient.get(dossierId) ?? {
        ouvert: false,
        canal: CanalNotification.EMAIL,
        message: '',
        enCours: false,
        succes: null,
        erreur: false
      }
    );
  }

  basculerNotifClient(dossierId: number): void {
    const etat = this.etatNotifClient(dossierId);
    this.etatsNotifClient.set(dossierId, { ...etat, ouvert: !etat.ouvert, succes: null, erreur: false });
  }

  modifierCanalNotifClient(dossierId: number, valeur: string): void {
    const etat = this.etatNotifClient(dossierId);
    this.etatsNotifClient.set(dossierId, { ...etat, canal: valeur as CanalNotification });
  }

  modifierMessageNotifClient(dossierId: number, valeur: string): void {
    const etat = this.etatNotifClient(dossierId);
    this.etatsNotifClient.set(dossierId, { ...etat, message: valeur });
  }

  peutNotifierClient(dossierId: number): boolean {
    return this.etatNotifClient(dossierId).message.trim().length > 0;
  }

  notifierClient(dossier: Dossier): void {
    const etat = this.etatNotifClient(dossier.dossierId);
    if (etat.enCours || !this.peutNotifierClient(dossier.dossierId)) return;

    this.etatsNotifClient.set(dossier.dossierId, { ...etat, enCours: true, erreur: false, succes: null });

    this.dossierApi
      .notifierClient(dossier.dossierId, { canal: etat.canal, message: etat.message })
      .subscribe({
        next: reponse => {
          this.etatsNotifClient.set(dossier.dossierId, {
            ...etat,
            enCours: false,
            erreur: false,
            succes: reponse.destinataire
          });
          this.cdr.detectChanges();
        },
        error: () => {
          this.etatsNotifClient.set(dossier.dossierId, { ...etat, enCours: false, erreur: true, succes: null });
          this.cdr.detectChanges();
        }
      });
  }
}