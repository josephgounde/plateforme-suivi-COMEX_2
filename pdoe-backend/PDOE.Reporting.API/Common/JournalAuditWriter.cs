using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;

namespace PDOE.Reporting.API.Common;

// Dupliqué dans chaque module (idem PDOE.Workflow.API) faute de ProjectReference cross-module.
internal static class JournalAuditWriter
{
    // EntiteId pointe l'ExportReglementaire déjà sauvegardé, pas besoin de redupliquer le chemin dans Description.
    public static void EnregistrerExport(PdoeDbContext db, ExportReglementaire export)
    {
        db.JournalAudit.Add(new JournalAudit
        {
            Categorie = "REPORTING",
            TypeAction = "EXPORT_RAPPORT",
            Description = $"Export {export.TypeExport} ({export.Categorie}) généré pour la période {export.DateDebut:dd/MM/yyyy} → {export.DateFin:dd/MM/yyyy}.",
            EntiteType = "ExportReglementaire",
            EntiteId = export.ExportReglementaireId.ToString(),
            DateAction = export.CreatedAt,
            CreatedBy = export.CreatedBy,
        });
    }
}
