using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Shared.Kernel.Common;
using PDOE.Workflow.API.Common;

namespace PDOE.Workflow.API.Features.RejeterDefinitif;

/// <summary>Réservé Direction/Admin DSIRI/Super Admin en théorie — non applicable sans PDOE.Gateway (Auth).</summary>
public class RejeterDefinitifHandler(PdoeDbContext db) : IRequestHandler<RejeterDefinitifCommand, WorkflowTransitionResponse>
{
    /// EXECUTE et tout statut en aval — l'opération est déjà partie, plus rien à annuler.
    private static readonly HashSet<StatutDossier> StatutsDejaExecutes =
    [
        StatutDossier.EXECUTE,
        StatutDossier.EN_APUREMENT,
        StatutDossier.APUREMENT_PARTIEL,
        StatutDossier.ALERTE_J14,
        StatutDossier.ALERTE_J8,
        StatutDossier.DEPASSE_BCEAO,
        StatutDossier.APURE,
        StatutDossier.EN_ARCHIVAGE,
        StatutDossier.ARCHIVE,
        StatutDossier.REJETE_DEFINITIF,
    ];

    public async Task<WorkflowTransitionResponse> Handle(RejeterDefinitifCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Request.Motif))
            throw new DomainException(400, ErrorResponseCode.MOTIF_REJET_MANQUANT, "motif requis.");

        var dossier = await db.Dossiers
            .Include(d => d.EtapesWorkflow)
            .FirstOrDefaultAsync(d => d.DossierId == command.DossierId, cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        var statutActuel = Enum.Parse<StatutDossier>(dossier.StatutElectronique);
        if (StatutsDejaExecutes.Contains(statutActuel))
        {
            throw new DomainException(409, ErrorResponseCode.STATUT_INVALIDE_POUR_ACTION,
                "Dossier déjà exécuté — rejet définitif impossible.");
        }

        var now = DateTime.UtcNow;
        var statutAvant = dossier.StatutElectronique;

        dossier.StatutElectronique = nameof(StatutDossier.REJETE_DEFINITIF);
        dossier.UpdatedAt = now;
        dossier.UpdatedBy = CurrentUser.Login;

        var etape = new EtapeWorkflow
        {
            DossierId = dossier.DossierId,
            // Pas de vrai code WorkflowEtapes ici — le rejet définitif peut partir de n'importe quel statut, pas juste une étape ETAPE_N.
            NiveauValidation = "REJET_DEFINITIF",
            StatutAvant = statutAvant,
            StatutApres = dossier.StatutElectronique,
            Action = nameof(ActionWorkflow.REJET_DEFINITIF),
            MotifRejet = command.Request.Motif,
            // NULL passerait la contrainte CK (elle ne vise que REJET), mais "AUCUN" reste plus clair : pas de vraie cible sur un rejet terminal.
            ResponsableCorrection = "AUCUN",
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
            StatutAvant = Enum.Parse<StatutDossier>(statutAvant),
            StatutApres = StatutDossier.REJETE_DEFINITIF,
            Action = ActionWorkflow.REJET_DEFINITIF,
            DateAction = now,
        };
    }
}
