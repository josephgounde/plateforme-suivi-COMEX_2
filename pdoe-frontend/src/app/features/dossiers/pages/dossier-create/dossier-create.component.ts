// Création (pas de :id, signature ABS2000 obligatoire avant saisie) et édition (:id, dossier BROUILLON déjà vérifié).
// Trois modes de signature : AUTOMATIQUE, VISUEL, LES_DEUX.

import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { DossierApiService } from '../../../../core/api/dossier-api.service';
import { ParametrageApiService } from '../../../../core/api/parametrage-api.service';
import { ToastService } from '../../../../core/toast/toast.service';
import {
  CreateDossierRequest,
  Document as PdoeDocument,
  DossierDetail,
  SignatureVerificationResult,
  UpdateDossierRequest
} from '../../../../core/models/dossier.model';
import {
  ModeVerificationSignature,
  TypeOperation,
  TYPE_OPERATION_LABELS,
  TypeCompte,
  TYPE_COMPTE_LABELS,
  QualiteResidence,
  QUALITE_RESIDENCE_LABELS,
  TypeDocument,
  TYPE_DOCUMENT_LABELS,
  referenceDocumentPlaceholder
} from '../../../../core/models/enums.model';
import { REGLES_APUREMENT, RegleApurement } from '../../../../core/models/regles-apurement.model';
import { PAYS_OPTIONS } from '../../../../core/models/pays.model';
import { DropdownSelectComponent, DropdownOption } from '../../../../shared/components/dropdown-select/dropdown-select.component';

type EtapeSignature = 'saisie' | 'verification' | 'confirmation_visuelle' | 'validee' | 'echec';

interface FichierEnAttente {
  fichier: File;
  typeDocument: TypeDocument;
  referenceDocument?: string;
}

interface ChecklistItem {
  document: string;
  etat: 'Obligatoire' | 'Facultatif' | 'Non requis';
}

// Documents attendus à l'Initiation — seuls IMPORT/SERVICE sont couverts par la table métier ; EXPORT/TRANSFERT
// n'ont pas encore de règle connue (checklistDocuments renvoie [] plutôt que d'inventer une liste).
const DOCUMENTS_UPLOAD_DISPONIBLES: TypeDocument[] = [
  TypeDocument.ORDRE_TRANSFERT,
  TypeDocument.FACTURE_PROFORMA,
  TypeDocument.FACTURE_DEFINITIVE,
  TypeDocument.FDI,
  TypeDocument.FORMULAIRE_CHANGE,
  TypeDocument.ATTESTATION_CHANGE,
  TypeDocument.CONTRAT,
  TypeDocument.ATTESTATION_BNC,
  TypeDocument.AUTRE
];

@Component({
  selector: 'app-dossier-create',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, FormsModule, DropdownSelectComponent],
  templateUrl: './dossier-create.component.html',
  styleUrl: './dossier-create.component.scss'
})
export class DossierCreateComponent implements OnInit {
  // null tant que la route n'a pas été lue dans ngOnInit — évite
  // de présumer le mode avant d'avoir vérifié le paramètre :id.
  modeEdition = false;
  dossierIdEdition: number | null = null;

  chargementInitial = false; // true uniquement en mode édition, le temps du GET
  enregistrement = false;
  erreurEnregistrement = false;

  // ── État du flux de signature (mode création uniquement) ───
  etapeSignature: EtapeSignature = 'saisie';
  resultatVerification: SignatureVerificationResult | null = null;
  confirmationVisuelleCochee = false;
  initialesAgent = '';

  formulaire: FormGroup;

  readonly typesOperation = Object.values(TypeOperation);
  readonly typeLabels = TYPE_OPERATION_LABELS;
  readonly typeOperationOptions: DropdownOption[] = this.typesOperation.map(t => ({ value: t, label: this.typeLabels[t] }));
  readonly typesCompte = Object.values(TypeCompte);
  readonly typeCompteLabels = TYPE_COMPTE_LABELS;
  readonly typeCompteOptions: DropdownOption[] = this.typesCompte.map(t => ({ value: t, label: this.typeCompteLabels[t] }));
  readonly qualitesResidence = Object.values(QualiteResidence);
  readonly qualiteResidenceLabels = QUALITE_RESIDENCE_LABELS;
  readonly qualiteResidenceOptions: DropdownOption[] = this.qualitesResidence.map(q => ({ value: q, label: this.qualiteResidenceLabels[q] }));
  readonly paysOptions: DropdownOption[] = PAYS_OPTIONS;
  readonly ModeVerificationSignature = ModeVerificationSignature;

  // Documents : création = mise en attente + envoi groupé après ; édition = envoi immédiat.
  readonly typeDocumentLabels = TYPE_DOCUMENT_LABELS;
  readonly documentsUploadDisponibles = DOCUMENTS_UPLOAD_DISPONIBLES;
  readonly documentsUploadOptions: DropdownOption[] = this.documentsUploadDisponibles.map(
    t => ({ value: t, label: this.typeDocumentLabels[t] })
  );
  typeDocumentSelectionne: TypeDocument = TypeDocument.ORDRE_TRANSFERT;
  fichierSelectionne: File | null = null;
  referenceDocumentSaisie = '';
  fichiersEnAttente: FichierEnAttente[] = [];
  documentsExistants: PdoeDocument[] = [];
  uploadEnCours = false;
  uploadErreur = false;

  get referenceDocumentPlaceholderActuel(): string {
    return referenceDocumentPlaceholder(this.typeDocumentSelectionne);
  }

  // ── Checklist documentaire dynamique (cf. table métier fournie) ─
  private seuilDomiciliationFcfa: number | null = null;
  private seuilFdiMontant: number | null = null;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private dossierApi: DossierApiService,
    private parametrageApi: ParametrageApiService,
    private toast: ToastService,
    private cdr: ChangeDetectorRef
  ) {
    this.formulaire = this.fb.group({
      numCompte: ['', Validators.required],
      matriculeClient: ['', Validators.required],
      nomClient: ['', Validators.required],
      nomBeneficiaire: ['', Validators.required],
      typeOperation: ['', Validators.required],
      natureTransaction: ['', Validators.required],
      referenceDomiciliation: [''],
      codeStatistiqueOperateur: [''],
      nifClient: [''],
      adressePostaleClient: [''],
      adresseGeographiqueClient: [''],
      telephoneClient: [''],
      codeBanque: [''],
      qualiteResidence: [''],
      dateOuvertureCompte: [''],
      anneeExerciceCompte: [''],
      montant: ['', [Validators.required, Validators.min(1)]],
      devise: ['XOF', Validators.required],
      paysBeneficiaire: ['', Validators.required],
      motif: ['', Validators.required],
      typeCompteDebite: ['', Validators.required],
      codeSwiftIndicatif: [''],
      banqueCorrespondanteIndicative: ['']
    });
  }

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');

    this.parametrageApi.get('SEUIL_DOMICILIATION_FCFA').subscribe({
      next: p => {
        this.seuilDomiciliationFcfa = Number(p.valeur);
        this.cdr.detectChanges();
      }
    });

    this.parametrageApi.get('SEUIL_FDI_MONTANT').subscribe({
      next: p => {
        this.seuilFdiMontant = Number(p.valeur);
        this.cdr.detectChanges();
      }
    });

    if (idParam) {
      this.modeEdition = true;
      this.dossierIdEdition = Number(idParam);
      this.chargerDossierExistant(this.dossierIdEdition);
    } else {
      // numCompte reste seul actif tant que la signature n'est pas validée.
      this.verrouillerChampsMetier(true);
    }
  }

  private chargerDossierExistant(id: number): void {
    this.chargementInitial = true;
    this.dossierApi.getDossier(id).subscribe({
      next: (dossier: DossierDetail) => {
        this.formulaire.patchValue({
          numCompte: dossier.numCompte,
          matriculeClient: dossier.matriculeClient,
          nomClient: dossier.nomClient,
          nomBeneficiaire: dossier.nomBeneficiaire,
          typeOperation: dossier.typeOperation,
          natureTransaction: dossier.natureTransaction,
          referenceDomiciliation: dossier.referenceDomiciliation,
          codeStatistiqueOperateur: dossier.codeStatistiqueOperateur,
          nifClient: dossier.nifClient,
          adressePostaleClient: dossier.adressePostaleClient,
          adresseGeographiqueClient: dossier.adresseGeographiqueClient,
          telephoneClient: dossier.telephoneClient,
          codeBanque: dossier.codeBanque,
          qualiteResidence: dossier.qualiteResidence,
          dateOuvertureCompte: dossier.dateOuvertureCompte,
          anneeExerciceCompte: dossier.anneeExerciceCompte,
          montant: dossier.montant,
          devise: dossier.devise,
          paysBeneficiaire: dossier.paysBeneficiaire,
          motif: dossier.motif,
          typeCompteDebite: dossier.typeCompteDebite,
          codeSwiftIndicatif: dossier.codeSwiftIndicatif,
          banqueCorrespondanteIndicative: dossier.banqueCorrespondanteIndicative
        });
        // numCompte n'est modifiable qu'à la création — la signature a été vérifiée pour CE compte.
        this.formulaire.get('numCompte')?.disable();
        this.documentsExistants = dossier.documents;
        this.chargementInitial = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.chargementInitial = false;
        this.cdr.detectChanges();
      }
    });
  }

  // ── Verrouillage des champs métier (mode création) ──────────

  private verrouillerChampsMetier(verrouille: boolean): void {
    const champs = [
      'matriculeClient', 'nomClient', 'nomBeneficiaire', 'typeOperation', 'natureTransaction',
      'referenceDomiciliation', 'codeStatistiqueOperateur', 'nifClient',
      'adressePostaleClient', 'adresseGeographiqueClient', 'telephoneClient',
      'codeBanque', 'qualiteResidence', 'dateOuvertureCompte', 'anneeExerciceCompte',
      'montant', 'devise', 'paysBeneficiaire', 'motif', 'typeCompteDebite',
      'codeSwiftIndicatif', 'banqueCorrespondanteIndicative'
    ];
    champs.forEach(c => {
      if (verrouille) {
        this.formulaire.get(c)?.disable();
      } else {
        this.formulaire.get(c)?.enable();
      }
    });
  }

  // ── Flux de vérification signature (mode création) ──────────

  lancerVerificationSignature(): void {
    const numCompte = this.formulaire.get('numCompte')?.value;
    if (!numCompte) {
      return;
    }

    this.etapeSignature = 'verification';

    this.dossierApi.verifierSignature(numCompte).subscribe({
      next: resultat => {
        this.resultatVerification = resultat;
        this.prefillDonneesAbs(resultat);

        if (!resultat.signatureExistante) {
          this.etapeSignature = 'echec';
          return;
        }

        if (resultat.modeVerification === ModeVerificationSignature.AUTOMATIQUE) {
          // Rien d'autre à faire : la vérification API suffit.
          this.etapeSignature = 'validee';
          this.verrouillerChampsMetier(false);
        } else {
          // VISUEL ou LES_DEUX : l'agent doit comparer l'image et
          // confirmer manuellement avant de pouvoir continuer.
          this.etapeSignature = 'confirmation_visuelle';
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.etapeSignature = 'echec';
        this.cdr.detectChanges();
      }
    });
  }

  // ABS2000 connaît déjà ces infos client — pré-remplies mais restent éditables (suggestion, pas source de vérité imposée).
  // matriculeClient/codeBanque exclus : identifiants internes AFB/Afriland CI, pas des données ABS2000.
  private prefillDonneesAbs(resultat: SignatureVerificationResult): void {
    const donnees: Record<string, unknown> = {
      nomClient: resultat.nomClient,
      nifClient: resultat.nifClient,
      adressePostaleClient: resultat.adressePostaleClient,
      adresseGeographiqueClient: resultat.adresseGeographiqueClient,
      telephoneClient: resultat.telephoneClient,
      qualiteResidence: resultat.qualiteResidence,
      dateOuvertureCompte: resultat.dateOuvertureCompte,
      anneeExerciceCompte: resultat.anneeExerciceCompte
    };

    Object.entries(donnees).forEach(([cle, valeur]) => {
      if (valeur !== undefined && valeur !== null) {
        this.formulaire.get(cle)?.setValue(valeur);
      }
    });
  }

  confirmerSignatureVisuelle(): void {
    const numCompte = this.formulaire.get('numCompte')?.value;
    if (!numCompte || !this.confirmationVisuelleCochee || !this.initialesAgent.trim()) {
      return;
    }

    this.dossierApi.validerSignatureVisuelle(numCompte, this.initialesAgent.trim()).subscribe({
      next: reponse => {
        if (reponse.signatureValidee) {
          this.etapeSignature = 'validee';
          this.verrouillerChampsMetier(false);
        } else {
          this.etapeSignature = 'echec';
        }
        this.cdr.detectChanges();
      },
      error: () => {
        this.etapeSignature = 'echec';
        this.cdr.detectChanges();
      }
    });
  }

  recommencerVerification(): void {
    this.etapeSignature = 'saisie';
    this.resultatVerification = null;
    this.confirmationVisuelleCochee = false;
    this.initialesAgent = '';
    this.verrouillerChampsMetier(true);
  }

  // ── Soumission ────────────────────────────────────────────

  // Ne dépend plus de formulaire.valid : le bouton reste cliquable même formulaire invalide,
  // pour que enregistrer() puisse déclencher markAllAsTouched() et afficher les erreurs de champ.
  get peutEnregistrer(): boolean {
    if (this.modeEdition) {
      return true;
    }
    return this.etapeSignature === 'validee';
  }

  // Vrai si le champ est en erreur ET que l'utilisateur y a déjà touché — évite d'afficher
  // les astérisques comme des erreurs avant même la première interaction.
  champInvalide(nom: string): boolean {
    const controle = this.formulaire.get(nom);
    return !!controle && controle.invalid && (controle.touched || controle.dirty);
  }

  champErreur(nom: string): string {
    const controle = this.formulaire.get(nom);
    if (!controle?.errors) return '';
    if (controle.errors['required']) return 'Ce champ est obligatoire.';
    if (controle.errors['min']) return 'Le montant doit être supérieur à 0.';
    return '';
  }

  enregistrer(): void {
    if (!this.peutEnregistrer) {
      return;
    }

    if (this.formulaire.invalid) {
      this.formulaire.markAllAsTouched();
      return;
    }

    this.enregistrement = true;
    this.erreurEnregistrement = false;
    const valeurs = this.formulaire.getRawValue();

    if (this.modeEdition && this.dossierIdEdition) {
      const requete: UpdateDossierRequest = {
        typeOperation: valeurs.typeOperation,
        montant: valeurs.montant,
        devise: valeurs.devise,
        paysBeneficiaire: valeurs.paysBeneficiaire,
        motif: valeurs.motif,
        matriculeClient: valeurs.matriculeClient,
        nomBeneficiaire: valeurs.nomBeneficiaire,
        natureTransaction: valeurs.natureTransaction,
        referenceDomiciliation: valeurs.referenceDomiciliation || undefined,
        codeStatistiqueOperateur: valeurs.codeStatistiqueOperateur || undefined,
        nifClient: valeurs.nifClient || undefined,
        adressePostaleClient: valeurs.adressePostaleClient || undefined,
        adresseGeographiqueClient: valeurs.adresseGeographiqueClient || undefined,
        telephoneClient: valeurs.telephoneClient || undefined,
        codeBanque: valeurs.codeBanque || undefined,
        qualiteResidence: valeurs.qualiteResidence || undefined,
        dateOuvertureCompte: valeurs.dateOuvertureCompte || undefined,
        anneeExerciceCompte: valeurs.anneeExerciceCompte || undefined,
        typeCompteDebite: valeurs.typeCompteDebite,
        codeSwiftIndicatif: valeurs.codeSwiftIndicatif || undefined,
        banqueCorrespondanteIndicative: valeurs.banqueCorrespondanteIndicative || undefined
      };

      this.dossierApi.updateDossier(this.dossierIdEdition, requete).subscribe({
        next: () => this.router.navigate(['/dossiers', this.dossierIdEdition]),
        error: () => {
          this.enregistrement = false;
          this.erreurEnregistrement = true;
          this.cdr.detectChanges();
        }
      });
      return;
    }

    const requete: CreateDossierRequest = {
      numCompte: valeurs.numCompte,
      nomClient: valeurs.nomClient,
      typeOperation: valeurs.typeOperation,
      montant: valeurs.montant,
      devise: valeurs.devise,
      paysBeneficiaire: valeurs.paysBeneficiaire,
      motif: valeurs.motif,
      matriculeClient: valeurs.matriculeClient,
      nomBeneficiaire: valeurs.nomBeneficiaire,
      natureTransaction: valeurs.natureTransaction,
      referenceDomiciliation: valeurs.referenceDomiciliation || undefined,
      codeStatistiqueOperateur: valeurs.codeStatistiqueOperateur || undefined,
      nifClient: valeurs.nifClient || undefined,
      adressePostaleClient: valeurs.adressePostaleClient || undefined,
      adresseGeographiqueClient: valeurs.adresseGeographiqueClient || undefined,
      telephoneClient: valeurs.telephoneClient || undefined,
      codeBanque: valeurs.codeBanque || undefined,
      qualiteResidence: valeurs.qualiteResidence || undefined,
      dateOuvertureCompte: valeurs.dateOuvertureCompte || undefined,
      anneeExerciceCompte: valeurs.anneeExerciceCompte || undefined,
      typeCompteDebite: valeurs.typeCompteDebite,
      codeSwiftIndicatif: valeurs.codeSwiftIndicatif || undefined,
      banqueCorrespondanteIndicative: valeurs.banqueCorrespondanteIndicative || undefined,
      signatureValideeABS: true,
      dateValidationSignature: new Date().toISOString(),
      modeVerificationApplique:
        this.resultatVerification?.modeVerification ?? ModeVerificationSignature.AUTOMATIQUE,
      signatureVerifieeVisuellement: this.etapeSignature === 'validee' && !!this.initialesAgent,
      initialesAgent: this.initialesAgent || undefined
    };

    this.dossierApi.createDossier(requete).subscribe({
      next: dossier => {
        // Retour au dashboard (Brouillons), pas à la fiche détail : c'est là que vit "Soumettre" pour un dossier neuf.
        const retourDashboard = () => {
          this.toast.succes('Dossier créé avec succès');
          this.router.navigate(['/dashboard']);
        };

        // Documents mis en attente localement (pas de dossierId avant création) : envoyés en parallèle avant de naviguer.
        if (this.fichiersEnAttente.length === 0) {
          retourDashboard();
          return;
        }
        forkJoin(
          this.fichiersEnAttente.map(f =>
            this.dossierApi.uploaderDocument(dossier.dossierId, f.fichier, f.typeDocument, false, f.referenceDocument)
          )
        ).subscribe({
          next: retourDashboard,
          error: retourDashboard
        });
      },
      error: () => {
        this.enregistrement = false;
        this.erreurEnregistrement = true;
        this.cdr.detectChanges();
      }
    });
  }

  // ── Documents justificatifs ───────────────────────────────────

  selectionnerFichier(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.fichierSelectionne = input.files?.[0] ?? null;
  }

  ajouterFichier(): void {
    if (!this.fichierSelectionne) return;

    if (this.modeEdition && this.dossierIdEdition) {
      this.envoyerFichierImmediat(this.dossierIdEdition);
      return;
    }

    this.fichiersEnAttente = [
      ...this.fichiersEnAttente,
      {
        fichier: this.fichierSelectionne,
        typeDocument: this.typeDocumentSelectionne,
        referenceDocument: this.referenceDocumentSaisie.trim() || undefined
      }
    ];
    this.fichierSelectionne = null;
    this.referenceDocumentSaisie = '';
  }

  retirerFichier(index: number): void {
    this.fichiersEnAttente = this.fichiersEnAttente.filter((_, i) => i !== index);
  }

  // Mode édition uniquement : le dossierId existe déjà, donc chaque fichier est envoyé directement (pas mis en attente).
  private envoyerFichierImmediat(dossierId: number): void {
    if (!this.fichierSelectionne) return;

    this.uploadEnCours = true;
    this.uploadErreur = false;
    this.dossierApi
      .uploaderDocument(
        dossierId,
        this.fichierSelectionne,
        this.typeDocumentSelectionne,
        false,
        this.referenceDocumentSaisie.trim() || undefined
      )
      .subscribe({
        next: document => {
          this.documentsExistants = [...this.documentsExistants, document];
          this.fichierSelectionne = null;
          this.referenceDocumentSaisie = '';
          this.uploadEnCours = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.uploadEnCours = false;
          this.uploadErreur = true;
          this.cdr.detectChanges();
        }
      });
  }

  // ── Règle réglementaire applicable (cf. REGLES_APUREMENT, Règlement
  // N° 09/2010/CM/UEMOA) — null tant qu'aucun type n'est sélectionné.
  get regleApurement(): RegleApurement | null {
    const type = this.formulaire.get('typeOperation')?.value as TypeOperation;
    return type ? REGLES_APUREMENT[type] ?? null : null;
  }

  // ── Checklist documentaire ── domiciliation informative seulement ; la FDI est réellement bloquante côté backend
  // (SoumettreDossierHandler), ce checklist-item n'est qu'un avertissement anticipé.
  get checklistDocuments(): ChecklistItem[] {
    const type = this.formulaire.get('typeOperation')?.value as TypeOperation;
    const regle = type ? REGLES_APUREMENT[type] : null;
    if (!regle) return [];

    const items: ChecklistItem[] = regle.justificatifs.map(j => ({ document: j.document, etat: j.etat }));
    const montant = Number(this.formulaire.get('montant')?.value) || 0;

    if (type === TypeOperation.IMPORT_BIENS || type === TypeOperation.EXPORT_BIENS) {
      const seuil = this.seuilDomiciliationFcfa ?? Infinity;
      items.push({
        document: 'Domiciliation bancaire (montant > 10 000 000 FCFA)',
        etat: montant >= seuil ? 'Obligatoire' : 'Non requis'
      });
    }

    if (type === TypeOperation.IMPORT_BIENS) {
      const seuilFdi = this.seuilFdiMontant ?? Infinity;
      items.push({
        document: "FDI (Fiche de Déclaration d'Importation)",
        etat: montant >= seuilFdi ? 'Obligatoire' : 'Non requis'
      });
    }

    return items;
  }
}