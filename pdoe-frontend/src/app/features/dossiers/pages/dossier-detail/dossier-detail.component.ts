// Vue de lecture complète d'un dossier (cible des liens "Examiner"/"Voir"). Read-only pour l'essentiel — rejet/upload
// restent sur les dashboards par rôle — sauf la confirmation de commande du Gestionnaire.

import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { DossierApiService } from '../../../../core/api/dossier-api.service';
import { WorkflowApiService } from '../../../../core/api/workflow-api.service';
import { DossierDetail, Document as PdoeDocument, SoldeClientResult } from '../../../../core/models/dossier.model';
import {
  StatutDossier,
  NiveauValidation,
  ProfilUtilisateur,
  TypeOperation,
  STATUT_LABELS,
  TYPE_OPERATION_LABELS,
  TYPE_DOCUMENT_LABELS,
  MODE_VERIFICATION_LABELS,
  TYPE_COMPTE_LABELS
} from '../../../../core/models/enums.model';
import { REGLES_APUREMENT, RegleApurement } from '../../../../core/models/regles-apurement.model';
import { WorkflowStepperComponent } from '../../../../shared/components/workflow-stepper/workflow-stepper.component';
import { DocumentPreviewModalComponent } from '../../../../shared/components/document-preview-modal/document-preview-modal.component';
import { declencherTelechargement } from '../../../../shared/utils/telechargement.util';
import { AuthService } from '../../../../core/auth/auth.service';
import { ToastService } from '../../../../core/toast/toast.service';

// Même sous-ensemble que GestionnaireDashboardComponent.STATUTS_FILE_ATTENTE — la confirmation/transmission n'a de
// sens que tant que le dossier est encore à l'étape Gestionnaire.
const STATUTS_FILE_ATTENTE_GESTIONNAIRE: ReadonlySet<StatutDossier> = new Set([
  StatutDossier.EN_VALIDATION_GESTIONNAIRE
]);

interface EtatConfirmation {
  confirme: boolean;
  date: string; // format yyyy-MM-dd, lié à l'input type="date"
  enregistrement: boolean;
  erreur: boolean;
}

interface EtatSolde {
  verification: boolean;
  resultat: SoldeClientResult | null;
  erreur: boolean;
}

@Component({
  selector: 'app-dossier-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, WorkflowStepperComponent, DocumentPreviewModalComponent],
  templateUrl: './dossier-detail.component.html',
  styleUrl: './dossier-detail.component.scss'
})
export class DossierDetailComponent implements OnInit {
  chargement = true;
  erreur = false;
  dossier: DossierDetail | null = null;
  exportFicheEnCours = false;
  exportHistoriqueEnCours = false;
  documentEnApercu: PdoeDocument | null = null;

  readonly statutLabels = STATUT_LABELS;
  readonly typeLabels = TYPE_OPERATION_LABELS;
  readonly typeDocumentLabels = TYPE_DOCUMENT_LABELS;
  readonly modeVerificationLabels = MODE_VERIFICATION_LABELS;
  readonly typeCompteLabels = TYPE_COMPTE_LABELS;

  // ── Confirmation de commande (Gestionnaire) ──────────────────
  etatConfirmation: EtatConfirmation = { confirme: false, date: '', enregistrement: false, erreur: false };
  transmissionEnCours = false;

  // ── Vérification du solde (Gestionnaire, étape 2) — ABS2000 en lecture seule ─
  etatSolde: EtatSolde = { verification: false, resultat: null, erreur: false };

  constructor(
    private route: ActivatedRoute,
    private dossierApi: DossierApiService,
    private workflowApi: WorkflowApiService,
    private cdr: ChangeDetectorRef,
    private auth: AuthService,
    private toast: ToastService
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));

    this.dossierApi.getDossier(id).subscribe({
      next: dossier => {
        this.dossier = dossier;
        this.etatConfirmation = {
          confirme: !!dossier.dateConfirmationClient,
          date: dossier.dateConfirmationClient ? dossier.dateConfirmationClient.slice(0, 10) : '',
          enregistrement: false,
          erreur: false
        };
        this.chargement = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.erreur = true;
        this.chargement = false;
        this.cdr.detectChanges();
      }
    });
  }

  // Visible uniquement pour le Gestionnaire tant que le dossier est encore à son étape — masqué une fois transmis.
  get peutConfirmerCommande(): boolean {
    return (
      this.auth.profil === ProfilUtilisateur.GESTIONNAIRE &&
      !!this.dossier &&
      STATUTS_FILE_ATTENTE_GESTIONNAIRE.has(this.dossier.statutElectronique)
    );
  }

  // Cocher pré-remplit la date du jour si le champ est vide ; l'agent peut toujours corriger.
  basculerConfirmation(): void {
    const etat = this.etatConfirmation;
    const nouvelEtat = !etat.confirme;

    this.etatConfirmation = {
      ...etat,
      confirme: nouvelEtat,
      date: nouvelEtat && !etat.date ? this.dateDuJourISO() : nouvelEtat ? etat.date : ''
    };
  }

  modifierDateConfirmation(valeur: string): void {
    this.etatConfirmation = { ...this.etatConfirmation, date: valeur };
  }

  enregistrerConfirmation(): void {
    const etat = this.etatConfirmation;
    if (!this.dossier || !etat.confirme || !etat.date) {
      return;
    }

    this.etatConfirmation = { ...etat, enregistrement: true, erreur: false };

    // dateConfirmationClient attend un ISO complet côté API ; input[type=date] ne donne que yyyy-MM-dd, complété à minuit.
    const dateIso = new Date(`${etat.date}T00:00:00`).toISOString();
    const dossier = this.dossier;

    this.dossierApi.updateDossier(dossier.dossierId, { dateConfirmationClient: dateIso }).subscribe({
      next: () => {
        this.etatConfirmation = { ...etat, enregistrement: false, erreur: false };
        dossier.dateConfirmationClient = dateIso;
        this.cdr.detectChanges();
      },
      error: () => {
        this.etatConfirmation = { ...etat, enregistrement: false, erreur: true };
        this.cdr.detectChanges();
      }
    });
  }

  private dateDuJourISO(): string {
    return new Date().toISOString().slice(0, 10);
  }

  // Consulte le solde ABS2000 (lecture seule) et marque soldeCompteVerifie sur le dossier — précondition
  // bloquante de peutValiderEtTransmettre(), au même titre que la confirmation de commande.
  verifierSolde(): void {
    const dossier = this.dossier;
    if (!dossier || this.etatSolde.verification) return;

    this.etatSolde = { ...this.etatSolde, verification: true, erreur: false };

    this.dossierApi.getSoldeClient(dossier.numCompte, dossier.dossierId).subscribe({
      next: resultat => {
        this.etatSolde = { verification: false, resultat, erreur: false };
        this.dossierApi.updateDossier(dossier.dossierId, { soldeCompteVerifie: true }).subscribe({
          next: () => {
            dossier.soldeCompteVerifie = true;
            this.cdr.detectChanges();
          }
        });
        this.cdr.detectChanges();
      },
      error: () => {
        this.etatSolde = { verification: false, resultat: null, erreur: true };
        this.cdr.detectChanges();
      }
    });
  }

  // Valide le dossier et le transmet à l'Agent COMEX — n'est proposé qu'après confirmation de commande
  // ET vérification du solde (les deux préconditions de la checklist étape 2). etatConfirmation.confirme
  // (la case cochée) est requis en plus de dateConfirmationClient (la valeur enregistrée) : décocher la case
  // doit rebloquer immédiatement même si une date avait déjà été sauvegardée précédemment — sinon la case
  // n'est plus qu'un affichage sans effet sur le blocage réel une fois une première confirmation enregistrée.
  peutValiderEtTransmettre(): boolean {
    return this.etatConfirmation.confirme && !!this.dossier?.dateConfirmationClient && !!this.dossier?.soldeCompteVerifie;
  }

  validerEtTransmettre(): void {
    if (!this.dossier || this.transmissionEnCours || !this.peutValiderEtTransmettre()) return;

    const dossier = this.dossier;
    this.transmissionEnCours = true;
    this.workflowApi
      .valider(dossier.dossierId, { niveauValidation: NiveauValidation.ETAPE_2_GESTIONNAIRE })
      .subscribe({
        next: reponse => {
          dossier.statutElectronique = reponse.statutApres;
          this.transmissionEnCours = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.transmissionEnCours = false;
          this.cdr.detectChanges();
        }
      });
  }

  // Étape 7 (Archivage) — ouverte à l'Agent COMEX et à l'Admin DSIRI, uniquement une fois sur EN_ARCHIVAGE.
  archivageEnCours = false;

  get peutArchiver(): boolean {
    return (
      (this.auth.profil === ProfilUtilisateur.AGENT_COMEX ||
        this.auth.profil === ProfilUtilisateur.ADMIN_DSIRI ||
        this.auth.profil === ProfilUtilisateur.SUPER_ADMIN) &&
      this.dossier?.statutElectronique === StatutDossier.EN_ARCHIVAGE
    );
  }

  // ── Règle réglementaire applicable (cf. REGLES_APUREMENT) — même référence que dossier-create, en lecture seule.
  get regleApurement(): RegleApurement | null {
    const type = this.dossier?.typeOperation;
    return type ? REGLES_APUREMENT[type] ?? null : null;
  }

  // Miroir illustratif de DELAI_PAIEMENT_EXPORT_BIENS_J (120j) — EXPORT_BIENS a une échéance en deux temps
  // (paiement puis rapatriement), affichée à titre indicatif seulement, aucune colonne dédiée n'est persistée.
  private static readonly JOURS_PAIEMENT_EXPORT_BIENS = 120;

  get echeancePaiementExport(): string | null {
    if (this.dossier?.typeOperation !== TypeOperation.EXPORT_BIENS || !this.dossier.dateExecution) {
      return null;
    }
    const date = new Date(this.dossier.dateExecution);
    date.setDate(date.getDate() + DossierDetailComponent.JOURS_PAIEMENT_EXPORT_BIENS);
    return date.toISOString().slice(0, 10);
  }

  archiverDossier(): void {
    if (!this.dossier || this.archivageEnCours || !this.peutArchiver) return;

    const dossier = this.dossier;
    this.archivageEnCours = true;
    this.workflowApi.archiver(dossier.dossierId).subscribe({
      next: reponse => {
        dossier.statutElectronique = reponse.statutApres;
        this.archivageEnCours = false;
        this.toast.succes(`Dossier ${dossier.referenceInterne} archivé.`);
        this.cdr.detectChanges();
      },
      error: () => {
        this.archivageEnCours = false;
        this.toast.erreur("Échec de l'archivage — réessayez.");
        this.cdr.detectChanges();
      }
    });
  }

  // Codes d'étape réellement présents dans l'historique — permet à WorkflowStepperComponent de distinguer une étape
  // "sautée" d'une étape vraiment franchie, plutôt que de supposer "complete" tout ce qui précède le statut courant.
  get etapesTraverseesCodes(): string[] {
    return this.dossier?.etapesWorkflow.map(e => e.niveauValidation) ?? [];
  }

  ouvrirApercu(document: PdoeDocument): void {
    this.documentEnApercu = document;
  }

  fermerApercu(): void {
    this.documentEnApercu = null;
  }

  formaterTaille(octets: number): string {
    if (octets < 1024) return `${octets} o`;
    if (octets < 1024 * 1024) return `${(octets / 1024).toFixed(0)} Ko`;
    return `${(octets / (1024 * 1024)).toFixed(1)} Mo`;
  }

  exporterFiche(): void {
    if (this.exportFicheEnCours || !this.dossier) return;

    this.exportFicheEnCours = true;
    this.dossierApi.exporterFiche(this.dossier.dossierId).subscribe({
      next: blob => {
        declencherTelechargement(blob, `fiche-dossier_${this.dossier!.referenceInterne}.pdf`);
        this.exportFicheEnCours = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.exportFicheEnCours = false;
        this.cdr.detectChanges();
      }
    });
  }

  exporterHistorique(): void {
    if (this.exportHistoriqueEnCours || !this.dossier) return;

    this.exportHistoriqueEnCours = true;
    this.workflowApi.exporterHistorique(this.dossier.dossierId).subscribe({
      next: blob => {
        declencherTelechargement(blob, `historique_${this.dossier!.referenceInterne}.pdf`);
        this.exportHistoriqueEnCours = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.exportHistoriqueEnCours = false;
        this.cdr.detectChanges();
      }
    });
  }
}