using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Apurement.API.Common;
using PDOE.Apurement.API.Mapping;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Apurement.API.Features.DeclarerDepassement;

/// Marque le dossier DEPASSE_BCEAO ; l'export CRPI FINEX lui-même reste côté PDOE.Reporting.API (pas encore fait).
public class DeclarerDepassementHandler(PdoeDbContext db) : IRequestHandler<DeclarerDepassementCommand, DossierResponse>
{
    private static readonly HashSet<StatutDossier> StatutsApurementEnCours =
    [
        StatutDossier.EXECUTE,
        StatutDossier.EN_APUREMENT,
        StatutDossier.APUREMENT_PARTIEL,
        StatutDossier.ALERTE_J14,
        StatutDossier.ALERTE_J8,
    ];

    public async Task<DossierResponse> Handle(DeclarerDepassementCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        if (request.MontantNonApure <= 0)
        {
            throw new DomainException(422, ErrorResponseCode.VALEUR_HORS_PLAGE,
                "montantNonApure doit être strictement positif.");
        }

        var now = DateTime.UtcNow;
        if (request.DateDeclaration.UtcDateTime > now)
        {
            throw new DomainException(422, ErrorResponseCode.VALEUR_HORS_PLAGE,
                "dateDeclaration ne peut pas être dans le futur.");
        }

        var dossier = await db.Dossiers
            .Include(d => d.EtapesWorkflow)
            .FirstOrDefaultAsync(d => d.DossierId == command.DossierId, cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        var statutActuel = Enum.Parse<StatutDossier>(dossier.StatutElectronique);
        if (!StatutsApurementEnCours.Contains(statutActuel))
        {
            throw new DomainException(422, ErrorResponseCode.STATUT_INVALIDE_POUR_ACTION,
                "Ce dossier n'est pas en phase d'apurement — déclaration de dépassement impossible.");
        }

        var statutAvant = dossier.StatutElectronique;

        dossier.SoldeRestantApurement = request.MontantNonApure;
        dossier.StatutElectronique = nameof(StatutDossier.DEPASSE_BCEAO);
        dossier.UpdatedAt = now;
        dossier.UpdatedBy = CurrentUser.Login;

        // La déclaration BCEAO EST l'action attendue à J0 — on marque juste l'alerte comme envoyée.
        var alerteJ0 = await db.AlertesApurement.FirstOrDefaultAsync(
            a => a.DossierId == dossier.DossierId && a.TypeAlerte == nameof(TypeAlerte.DEPASSEMENT_J0),
            cancellationToken);
        if (alerteJ0 is not null)
        {
            alerteJ0.Envoye = true;
            alerteJ0.DateEnvoi = request.DateDeclaration.UtcDateTime;
        }

        var etape = new EtapeWorkflow
        {
            DossierId = dossier.DossierId,
            NiveauValidation = "APUREMENT_DEPASSEMENT",
            StatutAvant = statutAvant,
            StatutApres = dossier.StatutElectronique,
            Action = nameof(ActionWorkflow.ESCALADE),
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
