// Couleur d'un StatutDossier pour les graphiques de répartition — mêmes groupes danger/warning/success
// que DirectionDashboardComponent.statutClass(), extrait ici pour être réutilisé par les vues "Mes statistiques"
// (Agent d'accueil / Gestionnaire / Trésorerie) sans dupliquer le mapping trois fois de plus.

import { StatutDossier } from '../../core/models/enums.model';

const STATUTS_DANGER: ReadonlySet<StatutDossier> = new Set([
  StatutDossier.ALERTE_J8,
  StatutDossier.DEPASSE_BCEAO,
  StatutDossier.REJETE_DEFINITIF,
  StatutDossier.ANTI_FRACTIONNEMENT_DETECTE
]);

const STATUTS_WARNING: ReadonlySet<StatutDossier> = new Set([
  StatutDossier.ALERTE_J14,
  StatutDossier.EN_EXECUTION_SWIFT
]);

const STATUTS_SUCCESS: ReadonlySet<StatutDossier> = new Set([
  StatutDossier.APURE,
  StatutDossier.ARCHIVE
]);

export function couleurStatutBadge(statut: StatutDossier): string {
  if (STATUTS_DANGER.has(statut)) return 'var(--pdoe-red)';
  if (STATUTS_WARNING.has(statut)) return '#e65100';
  if (STATUTS_SUCCESS.has(statut)) return '#2e7d32';
  return '#1565c0';
}
