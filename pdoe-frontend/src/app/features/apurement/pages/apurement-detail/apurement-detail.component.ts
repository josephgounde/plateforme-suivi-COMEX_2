// Page Apurement (SEQ-04) : échéance/solde, paiements partiels, checklist, alertes — quatre blocs sur une page
// plutôt que des onglets, pour que l'agent COMEX voie tout en même temps.

import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import jsPDF from 'jspdf';
import { DossierApiService } from '../../../../core/api/dossier-api.service';
import { ApurementApiService, ChecklistItem } from '../../../../core/api/apurement-api.service';
import { ChecklistConfigApiService } from '../../../../core/api/checklist-config-api.service';
import { ToastService } from '../../../../core/toast/toast.service';
import {
  DossierDetail,
  AlerteApurement,
  PaiementPartiel,
  CreatePaiementRequest
} from '../../../../core/models/dossier.model';
import { TypeAlerte, TypeDocument, TYPE_DOCUMENT_LABELS, referenceDocumentPlaceholder } from '../../../../core/models/enums.model';
import { declencherTelechargement } from '../../../../shared/utils/telechargement.util';
import { DropdownSelectComponent, DropdownOption } from '../../../../shared/components/dropdown-select/dropdown-select.component';

@Component({
  selector: 'app-apurement-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, FormsModule, DropdownSelectComponent],
  templateUrl: './apurement-detail.component.html',
  styleUrl: './apurement-detail.component.scss'
})
export class ApurementDetailComponent implements OnInit {
  chargement = true;
  erreur = false;
  dossierId!: number;
  dossier: DossierDetail | null = null;

  paiements: PaiementPartiel[] = [];
  alertes: AlerteApurement[] = [];

  // Chargée depuis ChecklistConfigApiService, plus une constante figée : l'Admin DSIRI peut éditer ces items sans déploiement.
  checklist: ChecklistItem[] = [];
  checklistEnCours = false;
  checklistErreur = false;
  checklistChargementErreur = false;

  afficherFormulairePaiement = false;
  paiementEnCours = false;
  paiementErreur = false;
  formulairePaiement: FormGroup;

  depassementEnCours = false;
  depassementErreur = false;

  // Justificatifs d'apurement — même mécanisme d'upload que ComexDashboardComponent, scopé à un seul dossier ici.
  afficherFormulaireDocument = false;
  documentSelectionne: File | null = null;
  documentType: TypeDocument = TypeDocument.JUSTIFICATIF_APUREMENT;
  referenceDocumentSaisie = '';
  documentEnCours = false;
  documentErreur = false;

  readonly TypeAlerte = TypeAlerte;
  readonly typeDocumentLabels = TYPE_DOCUMENT_LABELS;
  readonly typesDocumentDisponibles = Object.values(TypeDocument);
  readonly typeDocumentOptions: DropdownOption[] = this.typesDocumentDisponibles.map(
    type => ({ value: type, label: this.typeDocumentLabels[type] })
  );

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private dossierApi: DossierApiService,
    private apurementApi: ApurementApiService,
    private checklistConfigApi: ChecklistConfigApiService,
    private toast: ToastService,
    private fb: FormBuilder,
    private cdr: ChangeDetectorRef
  ) {
    this.formulairePaiement = this.fb.group({
      montantPaiement: ['', [Validators.required, Validators.min(0.01)]],
      devise: ['XOF', Validators.required],
      datePaiement: ['', Validators.required],
      referencePaiement: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.dossierId = Number(this.route.snapshot.paramMap.get('id'));
    this.chargerTout();
  }

  private chargerTout(): void {
    this.chargement = true;

    this.dossierApi.getDossier(this.dossierId).subscribe({
      next: dossier => {
        this.dossier = dossier;
        this.formulairePaiement.patchValue({ devise: dossier.devise });
        this.chargement = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.erreur = true;
        this.chargement = false;
        this.cdr.detectChanges();
      }
    });

    this.apurementApi.getPaiements(this.dossierId).subscribe({
      next: reponse => {
        this.paiements = reponse.items;
        this.cdr.detectChanges();
      }
    });

    this.apurementApi.getAlertes(this.dossierId).subscribe({
      next: alertes => {
        this.alertes = alertes;
        this.cdr.detectChanges();
      }
    });

    this.checklistConfigApi.list().subscribe({
      next: items => {
        this.checklist = items
          .filter(i => i.actif)
          .sort((a, b) => a.ordre - b.ordre)
          .map(i => ({ libelle: i.libelle, valide: false }));
        this.cdr.detectChanges();
      },
      error: () => {
        this.checklistChargementErreur = true;
        this.cdr.detectChanges();
      }
    });
  }

  // Lu directement depuis le dossier plutôt que recalculé depuis this.paiements, qui peut être incomplet (session précédente).
  get soldeRestant(): number {
    return this.dossier?.soldeRestantApurement ?? this.dossier?.montant ?? 0;
  }

  get estEnDepassement(): boolean {
    if (!this.dossier?.dateEcheanceApurement) return false;
    return new Date(this.dossier.dateEcheanceApurement) < new Date() && this.soldeRestant > 0;
  }

  get checklistComplete(): boolean {
    return this.checklist.every(i => i.valide);
  }

  // La clôture (statut APURE) dépend du solde restant côté backend — cocher la checklist seule ne suffit jamais.
  get pretPourCloture(): boolean {
    return this.checklistComplete && this.soldeRestant <= 0;
  }

  // ── Paiements ───────────────────────────────────────────────

  ajouterPaiement(): void {
    if (this.formulairePaiement.invalid) return;

    this.paiementEnCours = true;
    this.paiementErreur = false;
    const v = this.formulairePaiement.value;

    const requete: CreatePaiementRequest = {
      montantPaiement: Number(v.montantPaiement),
      devise: v.devise,
      datePaiement: v.datePaiement,
      referencePaiement: v.referencePaiement
    };

    this.apurementApi.createPaiement(this.dossierId, requete).subscribe({
      next: paiement => {
        this.paiements = [...this.paiements, paiement];
        if (this.dossier) this.dossier.soldeRestantApurement = paiement.soldeRestant;
        this.afficherFormulairePaiement = false;
        this.paiementEnCours = false;
        this.formulairePaiement.reset({ devise: this.dossier?.devise });
        this.cdr.detectChanges();
      },
      error: () => {
        this.paiementEnCours = false;
        this.paiementErreur = true;
        this.cdr.detectChanges();
      }
    });
  }

  // ── Justificatifs d'apurement ─────────────────────────────────

  get referenceDocumentPlaceholderActuel(): string {
    return referenceDocumentPlaceholder(this.documentType);
  }

  selectionnerDocument(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.documentSelectionne = input.files?.[0] ?? null;
  }

  envoyerDocument(): void {
    if (!this.documentSelectionne || this.documentEnCours) return;

    this.documentEnCours = true;
    this.documentErreur = false;

    this.dossierApi
      .uploaderDocument(this.dossierId, this.documentSelectionne, this.documentType, false, this.referenceDocumentSaisie.trim() || undefined)
      .subscribe({
        next: doc => {
          // Filet de sécurité en plus du garde documentEnCours : idempotent sur documentId contre un double callback.
          const dejaPresent = this.dossier?.documents.some(d => d.documentId === doc.documentId);
          if (!dejaPresent) this.dossier?.documents.push(doc);
          this.documentSelectionne = null;
          this.referenceDocumentSaisie = '';
          this.documentEnCours = false;
          this.afficherFormulaireDocument = false;
          this.toast.succes('Pièce jointe ajoutée');
          this.cdr.detectChanges();
        },
        error: () => {
          this.documentEnCours = false;
          this.documentErreur = true;
          this.cdr.detectChanges();
        }
      });
  }

  // ── Checklist ──────────────────────────────────────────────

  soumettreChecklist(): void {
    this.checklistEnCours = true;
    this.checklistErreur = false;

    this.apurementApi
      .validerChecklist(this.dossierId, this.checklist, this.checklistComplete)
      .subscribe({
        next: dossier => {
          this.checklistEnCours = false;

          // apurementComplet ne devient vrai que si le solde restant était déjà à zéro, sinon la checklist est juste enregistrée.
          if (dossier.apurementComplet) {
            this.toast.succes(
              `Dossier ${dossier.referenceInterne} apuré avec succès — solde entièrement justifié, ` +
              `checklist validée et dossier clôturé.`
            );
            this.router.navigate(['/dashboard']);
            return;
          }

          // Fusion, pas remplacement : la réponse est un Dossier "plat" sans documents, un remplacement les effacerait.
          if (this.dossier) this.dossier = { ...this.dossier, ...dossier };
          this.toast.succes("Checklist enregistrée — solde restant à apurer avant clôture");
          this.cdr.detectChanges();
        },
        error: () => {
          this.checklistEnCours = false;
          this.checklistErreur = true;
          this.cdr.detectChanges();
        }
      });
  }

  // Contrairement aux autres exports, celui-ci n'appelle aucun endpoint : généré directement depuis l'état déjà chargé
  // du composant, la checklist n'étant jamais persistée item par item côté backend.
  exporterChecklist(): void {
    if (!this.dossier) return;

    const doc = new jsPDF();
    let y = 20;

    doc.setFontSize(14);
    doc.text(`Checklist d'apurement — ${this.dossier.referenceInterne}`, 14, y);
    y += 7;
    doc.setFontSize(11);
    doc.text(this.dossier.nomClient, 14, y);
    y += 12;

    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text('Pièces reçues', 14, y);
    y += 8;
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10);
    this.checklist.forEach(item => {
      doc.text(`${item.valide ? '[x]' : '[ ]'} ${item.libelle}`, 18, y);
      y += 7;
    });

    y += 6;
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text('Paiements partiels', 14, y);
    y += 8;
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10);
    if (this.paiements.length === 0) {
      doc.text('Aucun paiement enregistré.', 18, y);
      y += 7;
    } else {
      this.paiements.forEach(p => {
        doc.text(
          `${new Date(p.datePaiement).toLocaleDateString('fr-FR')} — ${p.montantPaiement.toLocaleString('fr-FR')} ${p.devise} (${p.referencePaiement})`,
          18,
          y
        );
        y += 7;
      });
    }

    y += 6;
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text("Statut d'apurement", 14, y);
    y += 8;
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10);
    doc.text(`Montant du dossier : ${this.dossier.montant.toLocaleString('fr-FR')} ${this.dossier.devise}`, 18, y);
    y += 7;
    doc.text(`Solde restant : ${this.soldeRestant.toLocaleString('fr-FR')} ${this.dossier.devise}`, 18, y);
    y += 7;
    doc.text(`Checklist complète : ${this.checklistComplete ? 'Oui' : 'Non'}`, 18, y);
    y += 7;
    doc.text(`Apurement complet : ${this.dossier.apurementComplet ? 'Oui' : 'Non'}`, 18, y);

    declencherTelechargement(doc.output('blob'), `checklist-apurement_${this.dossier.referenceInterne}.pdf`);
  }

  // ── Dépassement J=0 ──────────────────────────────────────────

  declarerDepassement(): void {
    this.depassementEnCours = true;
    this.depassementErreur = false;

    this.apurementApi
      .declarerDepassement(this.dossierId, new Date().toISOString(), this.soldeRestant)
      .subscribe({
        next: dossier => {
          // Même raisonnement que soumettreChecklist() : fusionner, pas remplacer, pour ne pas perdre dossier.documents.
          if (this.dossier) this.dossier = { ...this.dossier, ...dossier };
          this.depassementEnCours = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.depassementEnCours = false;
          this.depassementErreur = true;
          this.cdr.detectChanges();
        }
      });
  }
}