namespace PDOE.Dossiers.API.Common;

// Portage C# de pdoe-frontend/src/app/core/models/regles-apurement.model.ts — table de référence
// réglementaire par TypeOperation, purement informative (les délais réels utilisés pour
// DateEcheanceApurement vivent dans ParametreMetier, DELAI_APUREMENT_*_J). Gardée synchronisée
// à la main avec le fichier frontend faute de source commune entre TS et C#.
internal static class ReglesApurement
{
    public static readonly IReadOnlyDictionary<string, RegleApurement> Table = new Dictionary<string, RegleApurement>
    {
        ["IMPORT_BIENS"] = new RegleApurement(
            "Domiciliation obligatoire si montant > 10 000 000 FCFA. Paiement à l'échéance contractuelle. Apurement sous 30 jours après le dédouanement.",
            "Règlement N° 09/2010/CM/UEMOA (Annexe II, Articles 3, 5, 10 & 12)"),
        ["IMPORT_SERVICES"] = new RegleApurement(
            "Paiement selon la facture / contrat. Apurement sous 30 jours suivant la réalisation du service ou la réception de la facture définitive.",
            "Règlement N° 09/2010/CM/UEMOA (Article 4 & Annexe II, Articles 10 & 12)"),
        ["EXPORT_BIENS"] = new RegleApurement(
            "Domiciliation obligatoire si montant > 10 000 000 FCFA. Échéance de paiement max 120 jours après expédition. Rapatriement des fonds sous 30 jours après l'exigibilité.",
            "Règlement N° 09/2010/CM/UEMOA (Annexe II, Articles 13, 15, 16 & 17)"),
        ["EXPORT_SERVICES"] = new RegleApurement(
            "Rapatriement des fonds obligatoire sous 30 jours à compter de la date d'exigibilité du paiement.",
            "Règlement N° 09/2010/CM/UEMOA (Annexe II, Article 15)"),
        ["TRANSFERT_CAPITAUX"] = new RegleApurement(
            "Déclaration / Autorisation préalable selon la nature du transfert. Apurement documentaire sous 30 jours après exécution.",
            "Règlement N° 09/2010/CM/UEMOA (Articles 6, 7, 10 & Titre IV)"),
    };
}

internal record RegleApurement(string DelaiLibelle, string ReferenceReglementaire);
