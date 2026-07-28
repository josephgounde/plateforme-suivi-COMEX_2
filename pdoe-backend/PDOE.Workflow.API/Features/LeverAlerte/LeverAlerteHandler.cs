using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Shared.Kernel.Common;
using PDOE.Workflow.API.Common;

namespace PDOE.Workflow.API.Features.LeverAlerte;

/// Réservé Direction/Admin en théorie, pas appliqué tant que PDOE.Gateway n'existe pas. Lève juste l'alerte posée par SignalerFractionnementHandler — la détection elle-même est externe à PDOE.
public class LeverAlerteHandler(PdoeDbContext db) : IRequestHandler<LeverAlerteCommand, WorkflowTransitionResponse>
{
    public async Task<WorkflowTransitionResponse> Handle(LeverAlerteCommand command, CancellationToken cancellationToken)
    {
        var dossier = await db.Dossiers
            .Include(d => d.EtapesWorkflow)
            .FirstOrDefaultAsync(d => d.DossierId == command.DossierId, cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        if (dossier.StatutElectronique != nameof(StatutDossier.ANTI_FRACTIONNEMENT_DETECTE))
        {
            throw new DomainException(422, ErrorResponseCode.STATUT_INVALIDE_POUR_ACTION,
                "Aucune alerte anti-fractionnement à lever sur ce dossier.");
        }

        var now = DateTime.UtcNow;
        var statutAvant = dossier.StatutElectronique;

        // Revient toujours vers EN_ATTENTE_EXECUTION, peu importe si on venait d'avant ou pendant l'exécution SWIFT.
        dossier.StatutElectronique = nameof(StatutDossier.EN_ATTENTE_EXECUTION);
        dossier.UpdatedAt = now;
        dossier.UpdatedBy = CurrentUser.Login;

        var etape = new EtapeWorkflow
        {
            DossierId = dossier.DossierId,
            NiveauValidation = "SIGNALEMENT_FRACTIONNEMENT",
            StatutAvant = statutAvant,
            StatutApres = dossier.StatutElectronique,
            Action = nameof(ActionWorkflow.LEVEE_ALERTE),
            AgentLogin = CurrentUser.Login,
            DateAction = now,
            CreatedAt = now,
            CreatedBy = CurrentUser.Login,
        };
        dossier.EtapesWorkflow.Add(etape);
        JournalAuditWriter.EnregistrerTransition(db, dossier, etape);

        await db.SaveChangesAsync(cancellationToken);

        return new WorkflowTransitionResponse
        {
            DossierId = dossier.DossierId,
            ReferenceInterne = dossier.ReferenceInterne,
            StatutAvant = StatutDossier.ANTI_FRACTIONNEMENT_DETECTE,
            StatutApres = StatutDossier.EN_ATTENTE_EXECUTION,
            Action = ActionWorkflow.LEVEE_ALERTE,
            DateAction = now,
        };
    }
}
