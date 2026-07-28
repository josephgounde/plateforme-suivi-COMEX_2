using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Execution.API.Common;
using PDOE.Execution.API.Mapping;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Execution.API.Features.BasculerExecution;

/// V1 : bascule manuelle par l'Agent COMEX après virement initié sur ABS2000/SWIFT (hors PDOE). V2 : API directe.
public class BasculerExecutionHandler(PdoeDbContext db) : IRequestHandler<BasculerExecutionCommand, DossierResponse>
{
    public async Task<DossierResponse> Handle(BasculerExecutionCommand command, CancellationToken cancellationToken)
    {
        var dossier = await db.Dossiers
            .Include(d => d.EtapesWorkflow)
            .FirstOrDefaultAsync(d => d.DossierId == command.DossierId, cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        if (dossier.StatutElectronique != nameof(StatutDossier.EN_ATTENTE_EXECUTION))
        {
            throw new DomainException(422, ErrorResponseCode.STATUT_INVALIDE_POUR_ACTION,
                "Le dossier n'est pas en statut EN_ATTENTE_EXECUTION — bascule impossible.");
        }

        var now = DateTime.UtcNow;
        var statutAvant = dossier.StatutElectronique;

        dossier.StatutElectronique = nameof(StatutDossier.EN_EXECUTION_SWIFT);
        dossier.UpdatedAt = now;
        dossier.UpdatedBy = CurrentUser.Login;

        var etape = new EtapeWorkflow
        {
            DossierId = dossier.DossierId,
            NiveauValidation = "ETAPE_5_EXECUTION",
            StatutAvant = statutAvant,
            StatutApres = dossier.StatutElectronique,
            Action = nameof(ActionWorkflow.BASCULE_SWIFT),
            AgentLogin = CurrentUser.Login,
            DateAction = now,
            CreatedAt = now,
            CreatedBy = CurrentUser.Login,
        };
        dossier.EtapesWorkflow.Add(etape);
        JournalAuditWriter.EnregistrerTransition(db, dossier, etape);

        await db.SaveChangesAsync(cancellationToken);

        return dossier.ToResponse();
    }
}
