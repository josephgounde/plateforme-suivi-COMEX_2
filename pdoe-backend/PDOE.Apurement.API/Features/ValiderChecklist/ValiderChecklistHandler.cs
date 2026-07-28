using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Apurement.API.Common;
using PDOE.Apurement.API.Mapping;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Apurement.API.Features.ValiderChecklist;

/// Pas de table pour les réponses de checklist par dossier — seul l'agrégat (tousValides + solde) pilote la transition, les items ne sont pas persistés.
public class ValiderChecklistHandler(PdoeDbContext db) : IRequestHandler<ValiderChecklistCommand, DossierResponse>
{
    private static readonly HashSet<StatutDossier> StatutsApurementEnCours =
    [
        StatutDossier.EXECUTE,
        StatutDossier.EN_APUREMENT,
        StatutDossier.APUREMENT_PARTIEL,
        StatutDossier.ALERTE_J14,
        StatutDossier.ALERTE_J8,
    ];

    public async Task<DossierResponse> Handle(ValiderChecklistCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        var dossier = await db.Dossiers
            .Include(d => d.EtapesWorkflow)
            .FirstOrDefaultAsync(d => d.DossierId == command.DossierId, cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        var statutActuel = Enum.Parse<StatutDossier>(dossier.StatutElectronique);
        if (!StatutsApurementEnCours.Contains(statutActuel))
        {
            throw new DomainException(422, ErrorResponseCode.STATUT_INVALIDE_POUR_ACTION,
                "Ce dossier n'est pas en phase d'apurement — validation de checklist impossible.");
        }

        var tousValides = request.TousValides || (request.Items.Count > 0 && request.Items.All(i => i.Valide));

        var now = DateTime.UtcNow;
        var statutAvant = dossier.StatutElectronique;

        if (tousValides && dossier.SoldeRestantApurement is 0)
        {
            dossier.StatutElectronique = nameof(StatutDossier.APURE);
            dossier.ApurementComplet = true;
            // Soldé et clôturé : on avance vers Archivage, sinon il reste coincé indéfiniment sur l'étape 6.
            await WorkflowEngine.AvancerVersEtapeSuivante(db, dossier, cancellationToken);
        }
        else if (dossier.SoldeRestantApurement.HasValue && dossier.MontantExecute.HasValue
                 && dossier.SoldeRestantApurement < dossier.MontantExecute)
        {
            dossier.StatutElectronique = nameof(StatutDossier.APUREMENT_PARTIEL);
        }
        else
        {
            dossier.StatutElectronique = nameof(StatutDossier.EN_APUREMENT);
        }

        dossier.UpdatedAt = now;
        dossier.UpdatedBy = CurrentUser.Login;

        var etape = new EtapeWorkflow
        {
            DossierId = dossier.DossierId,
            NiveauValidation = "APUREMENT_CHECKLIST",
            StatutAvant = statutAvant,
            StatutApres = dossier.StatutElectronique,
            Action = nameof(ActionWorkflow.RECEPTION_JUSTIFICATIF),
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
