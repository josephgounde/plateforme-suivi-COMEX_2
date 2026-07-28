using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Infrastructure.Notifications;
using PDOE.Shared.Kernel.Common;
using PDOE.Workflow.API.Common;

namespace PDOE.Workflow.API.Features.SignalerFractionnement;

/// Fractionnement détectable seulement côté ABS2000/SWIFT — cet endpoint enregistre juste le signalement de l'Agent COMEX à son retour.
public class SignalerFractionnementHandler(PdoeDbContext db, INotificationSender sender) : IRequestHandler<SignalerFractionnementCommand, WorkflowTransitionResponse>
{
    private static readonly HashSet<StatutDossier> StatutsFenetreExecution =
    [
        StatutDossier.EN_ATTENTE_EXECUTION,
        StatutDossier.EN_EXECUTION_SWIFT,
    ];

    public async Task<WorkflowTransitionResponse> Handle(SignalerFractionnementCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        if (string.IsNullOrWhiteSpace(request.Motif))
            throw new DomainException(400, ErrorResponseCode.MOTIF_REJET_MANQUANT, "motif requis.");

        var dossier = await db.Dossiers
            .Include(d => d.EtapesWorkflow)
            .FirstOrDefaultAsync(d => d.DossierId == command.DossierId, cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        var statutActuel = Enum.Parse<StatutDossier>(dossier.StatutElectronique);
        if (!StatutsFenetreExecution.Contains(statutActuel))
        {
            throw new DomainException(409, ErrorResponseCode.STATUT_INVALIDE_POUR_ACTION,
                "Dossier hors fenêtre d'exécution — signalement de fractionnement impossible.");
        }

        var now = DateTime.UtcNow;
        var statutAvant = dossier.StatutElectronique;

        dossier.StatutElectronique = nameof(StatutDossier.ANTI_FRACTIONNEMENT_DETECTE);
        dossier.UpdatedAt = now;
        dossier.UpdatedBy = CurrentUser.Login;

        var etape = new EtapeWorkflow
        {
            DossierId = dossier.DossierId,
            // Pas de vrai code WorkflowEtapes ici, même situation que RejeterDefinitif — hors des étapes ETAPE_N couvertes par WorkflowEngine.
            NiveauValidation = "SIGNALEMENT_FRACTIONNEMENT",
            StatutAvant = statutAvant,
            StatutApres = dossier.StatutElectronique,
            Action = nameof(ActionWorkflow.SIGNALEMENT_FRACTIONNEMENT),
            MotifRejet = request.Motif,
            AgentLogin = CurrentUser.Login,
            DateAction = now,
            CreatedAt = now,
            CreatedBy = CurrentUser.Login,
        };
        dossier.EtapesWorkflow.Add(etape);
        JournalAuditWriter.EnregistrerTransition(db, dossier, etape);

        // Direction et Admin DSIRI sont seuls habilités à lever l'alerte ou rejeter définitivement.
        await NotificationWriter.EnregistrerEtEnvoyer(
            db, sender, dossier.DossierId, "DOSSIER_FRACTIONNEMENT", "direction@afbci.ci", cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return new WorkflowTransitionResponse
        {
            DossierId = dossier.DossierId,
            ReferenceInterne = dossier.ReferenceInterne,
            StatutAvant = statutActuel,
            StatutApres = StatutDossier.ANTI_FRACTIONNEMENT_DETECTE,
            Action = ActionWorkflow.SIGNALEMENT_FRACTIONNEMENT,
            DateAction = now,
        };
    }
}
