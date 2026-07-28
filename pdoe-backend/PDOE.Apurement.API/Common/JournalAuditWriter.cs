using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;

namespace PDOE.Apurement.API.Common;

// Dupliqué dans chaque module faute de ProjectReference cross-module (cf. WorkflowEngine.cs).
// Miroir de MockDataService.ajouterEtape() côté frontend.
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
}
