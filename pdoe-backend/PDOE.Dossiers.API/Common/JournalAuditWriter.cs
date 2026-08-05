using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;

namespace PDOE.Dossiers.API.Common;

// Copié dans chaque module (pas de ProjectReference cross-module, cf. WorkflowEngine.cs).
// Reprend la logique de MockDataService.ajouterEtape() côté front.
internal static class JournalAuditWriter
{
    public static void EnregistrerTransition(PdoeDbContext db, Dossier dossier, EtapeWorkflow etape)
    {
        var motif = etape.MotifRejet is not null ? $" — motif : {etape.MotifRejet}" : "";
        db.JournalAudit.Add(new JournalAudit
        {
            Categorie = "WORKFLOW",
            TypeAction = etape.Action,
            Description = $"Dossier {dossier.ReferenceInterne} ({etape.NiveauValidation}) : {etape.StatutAvant} → {etape.StatutApres}{motif}.",
            EntiteType = "Dossier",
            EntiteId = dossier.DossierId.ToString(),
            DateAction = etape.DateAction,
            CreatedBy = etape.AgentLogin,
        });
    }

    // EntiteId pointe l'ExportReglementaire déjà sauvegardé, pas besoin de redupliquer le chemin dans Description.
    public static void EnregistrerExport(PdoeDbContext db, ExportReglementaire export, string referenceDossier)
    {
        db.JournalAudit.Add(new JournalAudit
        {
            Categorie = "REPORTING",
            TypeAction = "EXPORT_RAPPORT",
            Description = $"Export {export.TypeExport} du dossier {referenceDossier} généré.",
            EntiteType = "ExportReglementaire",
            EntiteId = export.ExportReglementaireId.ToString(),
            DateAction = export.CreatedAt,
            CreatedBy = export.CreatedBy,
        });
    }
}
