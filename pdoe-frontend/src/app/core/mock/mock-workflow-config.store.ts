// Étapes configurables du workflow (mock), reflète dbo.WorkflowEtapes.
// Ordre pilote le routage réel des dossiers, pas seulement l'affichage.

import { Injectable } from '@angular/core';
import { EtapeWorkflowConfig } from '../models/dossier.model';
import { TypeEtapeWorkflow } from '../models/enums.model';

const SEED_DATE = '2025-01-06T08:00:00.000Z';

@Injectable({ providedIn: 'root' })
export class MockWorkflowConfigStore {
  // Public et mutable : ce store EST la table, pas une façade en lecture seule.
  etapes: EtapeWorkflowConfig[] = [
    { etapeConfigId: 1, code: 'ETAPE_1_INITIATION', libelle: 'Initiation', ordre: 1, actif: true, typeEtape: TypeEtapeWorkflow.GENERIQUE, description: "Création du dossier par l'Agent d'accueil", createdAt: SEED_DATE, updatedAt: SEED_DATE, updatedBy: 'SYSTEM' },
    { etapeConfigId: 2, code: 'ETAPE_2_GESTIONNAIRE', libelle: 'Gestionnaire', ordre: 2, actif: true, typeEtape: TypeEtapeWorkflow.GESTIONNAIRE, description: 'Validation du Gestionnaire de compte', createdAt: SEED_DATE, updatedAt: SEED_DATE, updatedBy: 'SYSTEM' },
    { etapeConfigId: 3, code: 'ETAPE_3_COMEX', libelle: 'Contrôle COMEX', ordre: 3, actif: true, typeEtape: TypeEtapeWorkflow.COMEX, description: 'Contrôle réglementaire et LCB-FT par le COMEX', createdAt: SEED_DATE, updatedAt: SEED_DATE, updatedBy: 'SYSTEM' },
    { etapeConfigId: 4, code: 'ETAPE_4_TRESORERIE', libelle: 'Trésorerie', ordre: 4, actif: true, typeEtape: TypeEtapeWorkflow.TRESORERIE, description: 'Avis Trésorerie — taux de change, correspondant', createdAt: SEED_DATE, updatedAt: SEED_DATE, updatedBy: 'SYSTEM' },
    { etapeConfigId: 5, code: 'ETAPE_5_EXECUTION', libelle: 'Exécution', ordre: 5, actif: true, typeEtape: TypeEtapeWorkflow.EXECUTION, description: "Bascule et déclaration d'exécution SWIFT", createdAt: SEED_DATE, updatedAt: SEED_DATE, updatedBy: 'SYSTEM' },
    { etapeConfigId: 6, code: 'ETAPE_6_APUREMENT', libelle: 'Apurement', ordre: 6, actif: true, typeEtape: TypeEtapeWorkflow.APUREMENT, description: 'Suivi des justificatifs et échéance BCEAO', createdAt: SEED_DATE, updatedAt: SEED_DATE, updatedBy: 'SYSTEM' },
    { etapeConfigId: 7, code: 'ETAPE_7_ARCHIVAGE', libelle: 'Archivage', ordre: 7, actif: true, typeEtape: TypeEtapeWorkflow.GENERIQUE, description: 'Clôture et archivage du dossier', createdAt: SEED_DATE, updatedAt: SEED_DATE, updatedBy: 'SYSTEM' }
  ];

  prochainId = 8;

  findByCode(code: string): EtapeWorkflowConfig | undefined {
    return this.etapes.find(e => e.code === code);
  }

  // Utilisée par le moteur de transition pour dériver "l'étape suivante" — une étape désactivée est sautée, pas juste masquée.
  get etapesActives(): EtapeWorkflowConfig[] {
    return this.etapes.filter(e => e.actif).sort((a, b) => a.ordre - b.ordre);
  }
}
