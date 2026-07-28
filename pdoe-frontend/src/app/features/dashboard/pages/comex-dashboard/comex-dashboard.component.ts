// Dashboard Agent COMEX : Contrôle/Exécution/Apurement en trois sections filtrées par statut.
// L'exécution s'ouvre en modale, sans route dédiée.

import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { DossierApiService } from '../../../../core/api/dossier-api.service';
import { WorkflowApiService } from '../../../../core/api/workflow-api.service';
import { ParametrageApiService } from '../../../../core/api/parametrage-api.service';
import { ExecutionApiService } from '../../../../core/api/execution-api.service';
import { ReportingApiService } from '../../../../core/api/reporting-api.service';
import { ToastService } from '../../../../core/toast/toast.service';
import { declencherTelechargement } from '../../../../shared/utils/telechargement.util';
import { Dossier, DeclarerExecutionRequest } from '../../../../core/models/dossier.model';
import {
  StatutDossier,
  NiveauValidation,
  TypeDocument,
  TYPE_OPERATION_LABELS,
  TYPE_DOCUMENT_LABELS,
  STATUT_LABELS,
  referenceDocumentPlaceholder
} from '../../../../core/models/enums.model';
import { MockWorkflowConfigStore } from '../../../../core/mock/mock-workflow-config.store';
import { DelaiTraitementComponent } from '../../../../shared/components/delai-traitement/delai-traitement.component';
import { RepartitionChartComponent, RepartitionItem } from '../../../../shared/components/repartition-chart/repartition-chart.component';
import { NotificationPanelComponent } from '../../../../shared/components/notification-panel/notification-panel.component';
import { DashboardNavService } from '../../../../core/layout/dashboard-nav.service';
import { DropdownSelectComponent, DropdownOption } from '../../../../shared/components/dropdown-select/dropdown-select.component';
import { PagerComponent } from '../../../../shared/components/pager/pager.component';

// Dossiers par page, par section — la liste chargée en mémoire dépasse le défaut API (200 vs 20).
const PAGE_SIZE = 8;

const STATUTS_CONTROLE: ReadonlySet<StatutDossier> = new Set([
  StatutDossier.EN_CONTROLE_COMEX,
  StatutDossier.VALIDE_COMEX
]);

const STATUTS_EXECUTION: ReadonlySet<StatutDossier> = new Set([
  StatutDossier.EN_ATTENTE_EXECUTION,
  StatutDossier.EN_EXECUTION_SWIFT,
  StatutDossier.EXECUTE,
  // Le fractionnement se constate côté plateforme externe d'exécution ; le COMEX le signale
  // mais ne le résout pas (réservé à Direction/Admin DSIRI).
  StatutDossier.ANTI_FRACTIONNEMENT_DETECTE
]);

const STATUTS_APUREMENT: ReadonlySet<StatutDossier> = new Set([
  StatutDossier.EN_APUREMENT,
  StatutDossier.APUREMENT_PARTIEL,
  StatutDossier.ALERTE_J14,
  StatutDossier.ALERTE_J8,
  StatutDossier.DEPASSE_BCEAO
]);

const STATUTS_URGENTS: ReadonlySet<StatutDossier> = new Set([
  StatutDossier.ALERTE_J8,
  StatutDossier.DEPASSE_BCEAO
]);

interface EtatUpload {
  ouvert: boolean;
  fichier: File | null;
  typeDocument: TypeDocument;
  referenceDocument: string;
  enCours: boolean;
  succes: boolean;
  erreur: boolean;
}

// Un module = un onglet de la barre latérale qui remplace le contenu affiché, pas un ancrage de défilement.
type VueComex = 'controle' | 'execution' | 'apurement' | 'historique';

interface ModuleNav {
  vue: VueComex;
  icone: string;
  libelle: string;
  count: number;
}

@Component({
  selector: 'app-comex-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    ReactiveFormsModule,
    DelaiTraitementComponent,
    RepartitionChartComponent,
    NotificationPanelComponent,
    DropdownSelectComponent,
    PagerComponent
  ],
  templateUrl: './comex-dashboard.component.html',
  styleUrl: './comex-dashboard.component.scss'
})
export class ComexDashboardComponent implements OnInit, OnDestroy {
  chargement = true;
  dossiers: Dossier[] = [];

  etatsUpload = new Map<number, EtatUpload>();

  // Délai de l'étape Contrôle (ParametreMetier.DELAI_COMEX_HEURES) ; Exécution/Apurement n'ont pas d'équivalent.
  delaiHeures: number | null = null;

  transmissionEnCours = new Set<number>();

  // ── Exports réglementaires (DGI / Trésor / BCEAO) — même bloc que Direction/Admin
  reglementaireDateDebut = this.premierJourDuMois();
  reglementaireDateFin = this.aujourdHui();
  exportCrpiDgiEnCours = false;
  exportCrpiTresorEnCours = false;
  exportSituationBceaoEnCours = false;

  readonly typeLabels = TYPE_OPERATION_LABELS;
  readonly typeDocumentLabels = TYPE_DOCUMENT_LABELS;
  readonly statutLabels = STATUT_LABELS;
  readonly typesDocumentDisponibles = Object.values(TypeDocument);
  readonly typeDocumentOptions: DropdownOption[] = this.typesDocumentDisponibles.map(
    type => ({ value: type, label: this.typeDocumentLabels[type] })
  );

  readonly StatutDossier = StatutDossier;

  constructor(
    private dossierApi: DossierApiService,
    private workflowApi: WorkflowApiService,
    private cdr: ChangeDetectorRef,
    private workflowConfig: MockWorkflowConfigStore,
    private parametrageApi: ParametrageApiService,
    private executionApi: ExecutionApiService,
    private reporting: ReportingApiService,
    private toast: ToastService,
    private fb: FormBuilder,
    private dashboardNav: DashboardNavService
  ) {
    this.executionFormulaire = this.fb.group({
      referenceABS: ['', Validators.required],
      referenceSWIFT: ['', Validators.required],
      numeroAC: [''],
      codeTRF: [''],
      dateExecution: ['', Validators.required],
      montantExecute: ['', [Validators.required, Validators.min(0.01)]]
    });
  }

  // Onglet actif — porté par DashboardNavService, cf.
  // AgentAccueilDashboardComponent pour le raisonnement complet.
  get vueActive(): VueComex {
    return (this.dashboardNav.activeId() as VueComex | null) ?? 'controle';
  }

  ngOnInit(): void {
    // Ne force 'controle' qu'à la première entrée, sinon revenir via "Voir →" réinitialise l'onglet actif.
    const ongletsValides: ReadonlySet<VueComex> = new Set(['controle', 'execution', 'apurement', 'historique']);
    const ongletCourant = this.dashboardNav.activeId() as VueComex | null;
    if (!ongletCourant || !ongletsValides.has(ongletCourant)) {
      this.dashboardNav.select('controle');
    }
    this.dashboardNav.setItems(this.versNavItems());

    this.parametrageApi.get('DELAI_COMEX_HEURES').subscribe({
      next: p => {
        this.delaiHeures = Number(p.valeur);
        this.cdr.detectChanges();
      }
    });

    // pageSize relevé au-delà du défaut backend (20) : la pagination ci-dessous est client (cf. PAGE_SIZE).
    this.dossierApi.listDossiers({ pageSize: 200 }).subscribe({
      next: reponse => {
        this.dossiers = reponse.items;
        this.dossiers.forEach(d => {
          this.etatsUpload.set(d.dossierId, {
            ouvert: false,
            fichier: null,
            typeDocument: TypeDocument.FDI,
            referenceDocument: '',
            enCours: false,
            succes: false,
            erreur: false
          });
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
  }

  ngOnDestroy(): void {
    this.dashboardNav.clear();
  }

  private versNavItems() {
    return this.modulesNav.map(m => ({ id: m.vue, icone: m.icone, libelle: m.libelle, count: m.count }));
  }

  get dossiersControle(): Dossier[] {
    // !etapeGenerique : un dossier sur une étape personnalisée garde son statutElectronique figé à sa dernière valeur COMEX.
    return this.dossiers.filter(d => STATUTS_CONTROLE.has(d.statutElectronique) && !d.etapeGenerique);
  }

  get dossiersExecution(): Dossier[] {
    return this.dossiers.filter(d => STATUTS_EXECUTION.has(d.statutElectronique) && !d.etapeGenerique);
  }

  get dossiersApurement(): Dossier[] {
    return this.dossiers.filter(d => STATUTS_APUREMENT.has(d.statutElectronique) && !d.etapeGenerique);
  }

  // ── Pagination client par section ── une page par onglet : changer d'onglet ne doit pas perdre la position des autres.
  readonly pageSize = PAGE_SIZE;
  pageControle = 1;
  pageExecution = 1;
  pageApurement = 1;
  pageHistorique = 1;

  private paginer(liste: Dossier[], page: 'pageControle' | 'pageExecution' | 'pageApurement' | 'pageHistorique'): Dossier[] {
    const totalPages = Math.max(1, Math.ceil(liste.length / PAGE_SIZE));
    if (this[page] > totalPages) this[page] = totalPages;
    const debut = (this[page] - 1) * PAGE_SIZE;
    return liste.slice(debut, debut + PAGE_SIZE);
  }

  get dossiersControleAffiches(): Dossier[] {
    return this.paginer(this.dossiersControle, 'pageControle');
  }

  get dossiersExecutionAffiches(): Dossier[] {
    return this.paginer(this.dossiersExecution, 'pageExecution');
  }

  get dossiersApurementAffiches(): Dossier[] {
    return this.paginer(this.dossiersApurement, 'pageApurement');
  }

  get dossiersHistoriqueAffiches(): Dossier[] {
    return this.paginer(this.dossiersHistorique, 'pageHistorique');
  }

  // Répartition entre les 3 sous-files, en proportion relative (pas juste le total déjà affiché par <h2>).
  get repartitionQueues(): RepartitionItem[] {
    const compteurs = [
      { libelle: 'Contrôle', couleur: '#7e57c2', count: this.dossiersControle.length },
      { libelle: 'Exécution', couleur: '#ffa726', count: this.dossiersExecution.length },
      { libelle: 'Apurement', couleur: '#66bb6a', count: this.dossiersApurement.length }
    ];
    const max = Math.max(1, ...compteurs.map(c => c.count));
    return compteurs.map(c => ({ ...c, pourcentage: Math.round((c.count / max) * 100) }));
  }

  // Toujours les 4 modules, même à 0 : ce sont de vrais onglets de navigation, pas des ancres de défilement.
  get modulesNav(): ModuleNav[] {
    return [
      { vue: 'controle', icone: '🔍', libelle: 'Contrôle réglementaire', count: this.dossiersControle.length },
      { vue: 'execution', icone: '⚡', libelle: 'Exécution', count: this.dossiersExecution.length },
      { vue: 'apurement', icone: '✅', libelle: 'Apurement', count: this.dossiersApurement.length },
      { vue: 'historique', icone: '📚', libelle: 'Historique', count: this.dossiersHistorique.length }
    ];
  }

  // Dossiers déjà passés par les 3 sections mais archivés ou rejetés définitivement. Lecture seule, repliée par défaut.
  get dossiersHistorique(): Dossier[] {
    return this.dossiers.filter(d =>
      !STATUTS_CONTROLE.has(d.statutElectronique) &&
      !STATUTS_EXECUTION.has(d.statutElectronique) &&
      !STATUTS_APUREMENT.has(d.statutElectronique)
    );
  }

  estUrgent(dossier: Dossier): boolean {
    return STATUTS_URGENTS.has(dossier.statutElectronique);
  }

  // ── Upload de pièce jointe ──────────────────────────────────

  etatUpload(dossierId: number): EtatUpload {
    return (
      this.etatsUpload.get(dossierId) ?? {
        ouvert: false,
        fichier: null,
        typeDocument: TypeDocument.FDI,
        referenceDocument: '',
        enCours: false,
        succes: false,
        erreur: false
      }
    );
  }

  basculerPanneauUpload(dossierId: number): void {
    const etat = this.etatUpload(dossierId);
    this.etatsUpload.set(dossierId, { ...etat, ouvert: !etat.ouvert, succes: false, erreur: false });
  }

  selectionnerFichier(dossierId: number, event: Event): void {
    const input = event.target as HTMLInputElement;
    const fichier = input.files?.[0] ?? null;
    const etat = this.etatUpload(dossierId);
    this.etatsUpload.set(dossierId, { ...etat, fichier, succes: false, erreur: false });
  }

  changerTypeDocument(dossierId: number, valeur: string): void {
    const etat = this.etatUpload(dossierId);
    this.etatsUpload.set(dossierId, { ...etat, typeDocument: valeur as TypeDocument });
  }

  referenceDocumentPlaceholderPour(dossierId: number): string {
    return referenceDocumentPlaceholder(this.etatUpload(dossierId).typeDocument);
  }

  modifierReferenceDocument(dossierId: number, valeur: string): void {
    const etat = this.etatUpload(dossierId);
    this.etatsUpload.set(dossierId, { ...etat, referenceDocument: valeur });
  }

  envoyerDocument(dossierId: number): void {
    const etat = this.etatUpload(dossierId);
    if (!etat.fichier) {
      return;
    }

    this.etatsUpload.set(dossierId, { ...etat, enCours: true, erreur: false, succes: false });

    this.dossierApi
      .uploaderDocument(dossierId, etat.fichier, etat.typeDocument, false, etat.referenceDocument.trim() || undefined)
      .subscribe({
        next: () => {
          this.etatsUpload.set(dossierId, {
            ouvert: false,
            fichier: null,
            typeDocument: TypeDocument.FDI,
            referenceDocument: '',
            enCours: false,
            succes: true,
            erreur: false
          });
          this.cdr.detectChanges();
        },
        error: () => {
          this.etatsUpload.set(dossierId, { ...etat, enCours: false, erreur: true });
          this.cdr.detectChanges();
        }
      });
  }

  // ── Alerte anti-fractionnement (section Exécution) ── le COMEX ne fait que RAPPORTER ce qu'il constate
  // sur la plateforme externe ; il ne peut ni lever l'alerte ni rejeter définitivement (réservé à Direction/Admin DSIRI).

  signalementOuvert = new Set<number>();
  signalementEnCours = new Set<number>();
  private motifsSignalement = new Map<number, string>();

  basculerSignalement(dossierId: number): void {
    if (this.signalementOuvert.has(dossierId)) {
      this.signalementOuvert.delete(dossierId);
    } else {
      this.signalementOuvert.add(dossierId);
    }
  }

  motifSignalement(dossierId: number): string {
    return this.motifsSignalement.get(dossierId) ?? '';
  }

  modifierMotifSignalement(dossierId: number, valeur: string): void {
    this.motifsSignalement.set(dossierId, valeur);
  }

  // Appelé depuis la modale d'exécution — ferme la modale au succès, le formulaire de déclaration ne s'applique plus.
  signalerFractionnement(dossier: Dossier): void {
    if (this.signalementEnCours.has(dossier.dossierId)) return;
    const motif = this.motifSignalement(dossier.dossierId).trim();
    if (!motif) return;

    this.signalementEnCours.add(dossier.dossierId);
    this.workflowApi.signalerFractionnement(dossier.dossierId, motif).subscribe({
      next: reponse => {
        dossier.statutElectronique = reponse.statutApres;
        this.signalementEnCours.delete(dossier.dossierId);
        this.signalementOuvert.delete(dossier.dossierId);
        this.toast.succes(`Fractionnement signalé pour le dossier ${dossier.referenceInterne} — transmis à la Direction.`);
        this.fermerExecution();
        this.cdr.detectChanges();
      },
      error: () => {
        this.signalementEnCours.delete(dossier.dossierId);
        this.cdr.detectChanges();
      }
    });
  }

  // ── Validation (section Contrôle uniquement) ─────────────────

  validerEtTransmettre(dossier: Dossier): void {
    if (this.transmissionEnCours.has(dossier.dossierId)) return;

    this.transmissionEnCours.add(dossier.dossierId);
    this.workflowApi
      .valider(dossier.dossierId, { niveauValidation: NiveauValidation.ETAPE_3_COMEX })
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

  // ── Rejet pour correction (section Contrôle) ── deux cibles possibles : Agent d'accueil ou Gestionnaire.
  // Calculé depuis MockWorkflowConfigStore : une étape désactivée par l'Admin DSIRI n'apparaît plus comme cible.
  get ciblesRejetPossibles(): { valeur: string; libelle: string }[] {
    const codesConnus = new Set([NiveauValidation.ETAPE_1_INITIATION as string, NiveauValidation.ETAPE_2_GESTIONNAIRE as string]);
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
        niveauValidation: NiveauValidation.ETAPE_3_COMEX,
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

  // ── Exécution (section Exécution) ── modale, un seul dossier ouvert à la fois (pas de Map par dossierId ici).
  dossierExecutionOuvert: Dossier | null = null;
  executionFormulaire: FormGroup;
  basculeEnCours = false;
  declarationEnCours = false;
  declarationErreur = false;

  ouvrirExecution(dossier: Dossier): void {
    this.dossierExecutionOuvert = dossier;
    this.declarationErreur = false;
    this.executionFormulaire.reset({
      referenceABS: '',
      referenceSWIFT: '',
      numeroAC: '',
      codeTRF: '',
      dateExecution: '',
      montantExecute: dossier.montant
    });

    // Bascule automatique vers SWIFT (SEQ-03) : la modale s'ouvre directement sur le formulaire, sans bouton intermédiaire.
    if (dossier.statutElectronique === StatutDossier.EN_ATTENTE_EXECUTION) {
      this.basculeEnCours = true;
      this.executionApi.basculer(dossier.dossierId).subscribe({
        next: reponse => {
          dossier.statutElectronique = reponse.statutElectronique;
          this.basculeEnCours = false;
          this.toast.succes(`Dossier ${dossier.referenceInterne} basculé vers la plateforme d'exécution (SWIFT).`);
          this.cdr.detectChanges();
        },
        error: () => {
          this.basculeEnCours = false;
          this.fermerExecution();
          this.cdr.detectChanges();
        }
      });
    }
  }

  fermerExecution(): void {
    this.dossierExecutionOuvert = null;
    this.basculeEnCours = false;
    this.declarationEnCours = false;
    this.declarationErreur = false;
  }

  declarerExecution(): void {
    if (this.executionFormulaire.invalid || !this.dossierExecutionOuvert) {
      return;
    }

    const dossier = this.dossierExecutionOuvert;
    this.declarationEnCours = true;
    this.declarationErreur = false;
    const v = this.executionFormulaire.value;

    const requete: DeclarerExecutionRequest = {
      referenceABS: v.referenceABS,
      referenceSWIFT: v.referenceSWIFT,
      numeroAC: v.numeroAC || undefined,
      codeTRF: v.codeTRF || undefined,
      dateExecution: new Date(v.dateExecution).toISOString(),
      montantExecute: Number(v.montantExecute)
    };

    this.executionApi.declarer(dossier.dossierId, requete).subscribe({
      next: resultat => {
        dossier.statutElectronique = resultat.statutElectronique;
        dossier.referenceABS = resultat.referenceABS;
        dossier.referenceSWIFT = resultat.referenceSWIFT;
        dossier.numeroAC = resultat.numeroAC;
        dossier.codeTRF = resultat.codeTRF;
        dossier.dateExecution = resultat.dateExecution;
        dossier.dateEcheanceApurement = resultat.dateEcheanceApurement;
        dossier.montantExecute = requete.montantExecute;
        this.declarationEnCours = false;
        const echeance = resultat.dateEcheanceApurement
          ? new Date(resultat.dateEcheanceApurement).toLocaleDateString('fr-FR')
          : null;
        this.toast.succes(
          echeance
            ? `Exécution déclarée pour le dossier ${dossier.referenceInterne} — apurement à justifier avant le ${echeance}.`
            : `Exécution déclarée pour le dossier ${dossier.referenceInterne}.`
        );
        this.dashboardNav.setItems(this.versNavItems());
        this.cdr.detectChanges();
      },
      error: () => {
        this.declarationEnCours = false;
        this.declarationErreur = true;
        this.cdr.detectChanges();
      }
    });
  }

  // ── Exports réglementaires (DGI / Trésor / BCEAO) ─────────────

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
}