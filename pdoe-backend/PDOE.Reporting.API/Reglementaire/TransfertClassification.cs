using PDOE.Infrastructure.Entities;

namespace PDOE.Reporting.API.Reglementaire;

/// Émis vs reçu + zone UEMOA du bénéficiaire (cf. CDC_PDOE_v5.docx §3.1). EXPORT_BIENS/SERVICES = reçu, le reste = émis.
/// TRANSFERT_CAPITAUX est toujours émis : cette version de PDOE ne couvre que les transferts sortants
/// (import biens/services, dividendes, redevances, frais de services, remboursements de prêts extérieurs, RDI) —
/// aucun cas de transfert de capitaux entrant n'est dans le périmètre actuel.
public static class TransfertClassification
{
    private static readonly HashSet<string> TypesReception = ["EXPORT_BIENS", "EXPORT_SERVICES"];

    private static readonly HashSet<string> PaysUemoa = new(StringComparer.OrdinalIgnoreCase)
    {
        "Bénin", "Benin",
        "Burkina Faso",
        "Côte d'Ivoire", "Cote d'Ivoire",
        "Guinée-Bissau", "Guinee-Bissau", "Guinée Bissau",
        "Mali",
        "Niger",
        "Sénégal", "Senegal",
        "Togo",
    };

    public static bool EstReception(Dossier dossier) => TypesReception.Contains(dossier.TypeOperation);

    public static bool EstUemoa(string? pays) => pays is not null && PaysUemoa.Contains(pays);
}
