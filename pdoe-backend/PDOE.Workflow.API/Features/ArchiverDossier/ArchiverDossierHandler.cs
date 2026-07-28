using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Shared.Kernel.Common;
using PDOE.Workflow.API.Common;

namespace PDOE.Workflow.API.Features.ArchiverDossier;

/// Étape 7 (Archivage). Ne fait que la transition d'état pour l'instant — vérif SHA-256 et reporting BCEAO restent à brancher via PDOE.Reporting.API.
public class ArchiverDossierHandler(PdoeDbContext db) : IRequestHandler<ArchiverDossierCommand, WorkflowTransitionResponse>
{
    public async Task<WorkflowTransitionResponse> Handle(ArchiverDossierCommand command, CancellationToken cancellationToken)
    {
        var dossier = await db.Dossiers
            .Include(d => d.EtapesWorkflow)
            .FirstOrDefaultAsync(d => d.DossierId == command.DossierId, cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        if (dossier.StatutElectronique != nameof(StatutDossier.EN_ARCHIVAGE))
        {
            throw new DomainException(422, ErrorResponseCode.STATUT_INVALIDE_POUR_ACTION,
                "Le dossier n'est pas en statut EN_ARCHIVAGE — archivage impossible.");
        }

        var now = DateTime.UtcNow;
        var statutAvant = dossier.StatutElectronique;

        dossier.StatutElectronique = nameof(StatutDossier.ARCHIVE);
        dossier.UpdatedAt = now;
        dossier.UpdatedBy = CurrentUser.Login;

        var etape = new EtapeWorkflow
        {
            DossierId = dossier.DossierId,
            NiveauValidation = "ETAPE_7_ARCHIVAGE",
            StatutAvant = statutAvant,
            StatutApres = dossier.StatutElectronique,
            Action = nameof(ActionWorkflow.ARCHIVAGE),
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
            StatutAvant = StatutDossier.EN_ARCHIVAGE,
            StatutApres = StatutDossier.ARCHIVE,
            Action = ActionWorkflow.ARCHIVAGE,
            DateAction = now,
        };
    }
}
