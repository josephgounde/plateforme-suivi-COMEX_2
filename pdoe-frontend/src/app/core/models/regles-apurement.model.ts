// Table de référence réglementaire par TypeOperation — purement informatif côté UI, ne bloque rien.
// Les délais réels utilisés pour DateEcheanceApurement vivent dans ParametrageMetier (DELAI_APUREMENT_*_J).

import { TypeOperation } from './enums.model';

export interface JustificatifApurement {
  document: string;
  etat: 'Obligatoire' | 'Facultatif' | 'Non requis';
}

export interface RegleApurement {
  fluxDevises: 'Entrée' | 'Sortie' | 'Entrée / Sortie';
  ceQuiEstApure: string;
  delaiLibelle: string;
  justificatifs: JustificatifApurement[];
  referenceReglementaire: string;
}

export const REGLES_APUREMENT: Record<TypeOperation, RegleApurement> = {
  [TypeOperation.IMPORT_BIENS]: {
    fluxDevises: 'Sortie',
    ceQuiEstApure: 'Entrée effective des marchandises sur le territoire ivoirien et passage en douane.',
    delaiLibelle: "Domiciliation obligatoire si montant > 10 000 000 FCFA. Paiement à l'échéance contractuelle. Apurement sous 30 jours après le dédouanement.",
    justificatifs: [
      { document: "Attestation d'Importation / DDU", etat: 'Obligatoire' },
      { document: 'Bon à Enlever (BAE)', etat: 'Obligatoire' },
      { document: 'Titre de transport (LTA, Connaissement)', etat: 'Obligatoire' }
    ],
    referenceReglementaire: 'Règlement N° 09/2010/CM/UEMOA (Annexe II, Articles 3, 5, 10 & 12)'
  },
  [TypeOperation.IMPORT_SERVICES]: {
    fluxDevises: 'Sortie',
    ceQuiEstApure: 'Exécution effective de la prestation immatérielle et régularisation fiscale locale.',
    delaiLibelle: 'Paiement selon la facture / contrat. Apurement sous 30 jours suivant la réalisation du service ou la réception de la facture définitive.',
    justificatifs: [
      { document: 'Attestation de service fait', etat: 'Obligatoire' },
      { document: 'Facture définitive du prestataire', etat: 'Obligatoire' },
      { document: 'Preuve d’acquittement de la Retenue à la Source (Impôts CI)', etat: 'Obligatoire' }
    ],
    referenceReglementaire: 'Règlement N° 09/2010/CM/UEMOA (Article 4 & Annexe II, Articles 10 & 12)'
  },
  [TypeOperation.EXPORT_BIENS]: {
    fluxDevises: 'Entrée',
    ceQuiEstApure: 'Encaissement et rapatriement effectif de la contrepartie financière de la vente.',
    delaiLibelle: "Domiciliation obligatoire si montant > 10 000 000 FCFA. Échéance de paiement max 120 jours après expédition. Rapatriement des fonds sous 30 jours après l'exigibilité.",
    justificatifs: [
      { document: 'Engagement de Rapatriement (ER)', etat: 'Obligatoire' },
      { document: 'Déclaration Douane Export', etat: 'Obligatoire' },
      { document: 'Avis de crédit SWIFT de rapatriement', etat: 'Obligatoire' }
    ],
    referenceReglementaire: 'Règlement N° 09/2010/CM/UEMOA (Annexe II, Articles 13, 15, 16 & 17)'
  },
  [TypeOperation.EXPORT_SERVICES]: {
    fluxDevises: 'Entrée',
    ceQuiEstApure: 'Rapatriement des devises générées par des prestations fournies à des non-résidents.',
    delaiLibelle: "Rapatriement des fonds obligatoire sous 30 jours à compter de la date d'exigibilité du paiement.",
    justificatifs: [
      { document: 'Contrat de prestation', etat: 'Obligatoire' },
      { document: 'Facture émise', etat: 'Obligatoire' },
      { document: 'Avis de crédit SWIFT (Rapatriement)', etat: 'Obligatoire' }
    ],
    referenceReglementaire: 'Règlement N° 09/2010/CM/UEMOA (Annexe II, Article 15)'
  },
  [TypeOperation.TRANSFERT_CAPITAUX]: {
    fluxDevises: 'Entrée / Sortie',
    ceQuiEstApure: 'Conformité réglementaire, autorisation administrative et apurement fiscal.',
    delaiLibelle: 'Déclaration / Autorisation préalable selon la nature du transfert. Apurement documentaire sous 30 jours après exécution.',
    justificatifs: [
      { document: "PV d'Assemblée Générale (Dividendes)", etat: 'Obligatoire' },
      { document: 'Quitus Fiscal à jour', etat: 'Obligatoire' },
      { document: 'Autorisation préalable Ministère des Finances / BCEAO', etat: 'Obligatoire' }
    ],
    referenceReglementaire: 'Règlement N° 09/2010/CM/UEMOA (Articles 6, 7, 10 & Titre IV)'
  }
};
