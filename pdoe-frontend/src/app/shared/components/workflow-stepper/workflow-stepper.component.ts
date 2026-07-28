// Position d'un dossier dans le circuit à 7 étapes. `compact` bascule entre ligne dense et vue détaillée.

import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StatutDossier, NiveauValidation } from '../../../core/models/enums.model';
import { MockWorkflowConfigStore } from '../../../core/mock/mock-workflow-config.store';

export type EtapeEtat = 'complete' | 'courante' | 'a_venir' | 'alerte' | 'rejete' | 'ignoree';

export interface EtapeAffichage {
  niveau: NiveauValidation;
  numero: number;
  libelleCourt: string;
  etat: EtapeEtat;
}

// Ordre fixe des 7 étapes — sert à la fois à l'affichage et au
// calcul de complete/à_venir par comparaison d'index.
const ORDRE_ETAPES: { niveau: NiveauValidation; libelleCourt: string }[] = [
  { niveau: NiveauValidation.ETAPE_1_INITIATION, libelleCourt: 'Initiation' },
  { niveau: NiveauValidation.ETAPE_2_GESTIONNAIRE, libelleCourt: 'Gestionnaire' },
  { niveau: NiveauValidation.ETAPE_3_COMEX, libelleCourt: 'Contrôle COMEX' },
  { niveau: NiveauValidation.ETAPE_4_TRESORERIE, libelleCourt: 'Trésorerie' },
  { niveau: NiveauValidation.ETAPE_5_EXECUTION, libelleCourt: 'Exécution' },
  { niveau: NiveauValidation.ETAPE_6_APUREMENT, libelleCourt: 'Apurement' },
  { niveau: NiveauValidation.ETAPE_7_ARCHIVAGE, libelleCourt: 'Archivage' }
];

// StatutDossier → index d'étape (0-based). Exportée pour réutilisation par MockDataService.mockHistorique() afin que
// "Historique du circuit" et "Position dans le circuit" ne divergent pas en dérivant chacun leur propre notion.
export const STATUT_VERS_INDEX_ETAPE: Record<StatutDossier, number> = {
  [StatutDossier.BROUILLON]: 0,
  [StatutDossier.INITIE]: 0,
  [StatutDossier.EN_VALIDATION_GESTIONNAIRE]: 1,
  [StatutDossier.CONFIRME_GESTIONNAIRE]: 1,
  [StatutDossier.EN_CONTROLE_COMEX]: 2,
  [StatutDossier.VALIDE_COMEX]: 2,
  [StatutDossier.EN_AVIS_TRESORERIE]: 3,
  [StatutDossier.AVIS_TRESORERIE_DONNE]: 3,
  // Le fractionnement se constate sur la plateforme externe d'exécution — alerte de l'étape Exécution (4), pas Contrôle (2).
  [StatutDossier.ANTI_FRACTIONNEMENT_DETECTE]: 4,
  [StatutDossier.EN_ATTENTE_EXECUTION]: 4,
  [StatutDossier.EN_EXECUTION_SWIFT]: 4,
  [StatutDossier.EXECUTE]: 4,
  [StatutDossier.EN_APUREMENT]: 5,
  [StatutDossier.APUREMENT_PARTIEL]: 5,
  [StatutDossier.ALERTE_J14]: 5,
  [StatutDossier.ALERTE_J8]: 5,
  [StatutDossier.DEPASSE_BCEAO]: 5,
  [StatutDossier.APURE]: 5,
  [StatutDossier.EN_ARCHIVAGE]: 6,
  [StatutDossier.ARCHIVE]: 6,
  // Rejeté définitif n'appartient à aucune étape "normale" — traité
  // à part dans resoudreEtapeCourante() plutôt que mappé ici.
  [StatutDossier.REJETE_DEFINITIF]: -1
};

// Statuts qui doivent colorer l'étape courante en alerte plutôt
// qu'en couleur neutre "en cours".
const STATUTS_ALERTE: ReadonlySet<StatutDossier> = new Set([
  StatutDossier.ANTI_FRACTIONNEMENT_DETECTE,
  StatutDossier.ALERTE_J14,
  StatutDossier.ALERTE_J8,
  StatutDossier.DEPASSE_BCEAO
]);

@Component({
  selector: 'app-workflow-stepper',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './workflow-stepper.component.html',
  styleUrl: './workflow-stepper.component.scss'
})
export class WorkflowStepperComponent {
  // Statut du dossier à représenter — seule donnée réellement
  // requise, tout le reste se déduit.
  @Input({ required: true }) statut!: StatutDossier;

  // true → rendu dense pour liste, false → rendu détaillé avec libellés.
  @Input() compact = false;

  // Codes réellement présents dans l'historique de CE dossier — optionnel. Si fourni, une étape antérieure absente
  // de l'historique est rendue 'ignoree' (désactivée à l'époque) plutôt que 'complete' par défaut.
  @Input() etapesTraversees?: string[];

  constructor(private workflowConfig: MockWorkflowConfigStore) {}

  // Libellé lu depuis MockWorkflowConfigStore si renommé par l'Admin, sinon replié sur ORDRE_ETAPES. La POSITION reste
  // dérivée de STATUT_VERS_INDEX_ETAPE — seul l'intitulé peut différer.
  private libelle(e: { niveau: NiveauValidation; libelleCourt: string }): string {
    return this.workflowConfig.findByCode(e.niveau)?.libelle ?? e.libelleCourt;
  }

  get etapes(): EtapeAffichage[] {
    if (this.statut === StatutDossier.REJETE_DEFINITIF) {
      // Pas de position dans le circuit normal ; le rejet est signalé séparément par le template (rejeteDefinitif).
      return ORDRE_ETAPES.map((e, i) => ({
        niveau: e.niveau,
        numero: i + 1,
        libelleCourt: this.libelle(e),
        etat: 'a_venir' as EtapeEtat
      }));
    }

    const indexCourant = STATUT_VERS_INDEX_ETAPE[this.statut] ?? 0;
    const enAlerte = STATUTS_ALERTE.has(this.statut);
    const traversees = this.etapesTraversees;

    // EN_ARCHIVAGE et ARCHIVE partagent le même indexCourant (6, pas d'étape suivante) ; on décale le seuil "complete"
    // d'un cran pour ARCHIVE, sinon l'étape 7 resterait affichée "courante" au lieu de "complete".
    const seuilComplete = this.statut === StatutDossier.ARCHIVE ? indexCourant + 1 : indexCourant;

    return ORDRE_ETAPES.map((e, i) => {
      let etat: EtapeEtat;
      if (i < seuilComplete) {
        // i === 0 (Initiation) n'a jamais d'entrée d'historique propre et ne peut être désactivée — toujours "complete".
        etat = i > 0 && traversees && !traversees.includes(e.niveau) ? 'ignoree' : 'complete';
      } else if (i === indexCourant) {
        etat = enAlerte ? 'alerte' : 'courante';
      } else {
        etat = 'a_venir';
      }
      return { niveau: e.niveau, numero: i + 1, libelleCourt: this.libelle(e), etat };
    });
  }

  get rejeteDefinitif(): boolean {
    return this.statut === StatutDossier.REJETE_DEFINITIF;
  }
}