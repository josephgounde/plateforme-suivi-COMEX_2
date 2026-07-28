using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Workflow.API.Features.ControleReglementaire;

/// Stub — pas d'intégration BCEAO/FINEX réelle, renvoie toujours conforme. À remplacer quand PDOE.CBS.Integration sera branché.
public class ControleReglementaireHandler(PdoeDbContext db) : IRequestHandler<ControleReglementaireCommand, ControleReglementaireResult>
{
    public async Task<ControleReglementaireResult> Handle(ControleReglementaireCommand command, CancellationToken cancellationToken)
    {
        var dossierExiste = await db.Dossiers.AnyAsync(d => d.DossierId == command.DossierId, cancellationToken);
        if (!dossierExiste)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        return new ControleReglementaireResult
        {
            Conforme = true,
            PlafondRespecte = true,
            CodeRetour = "OK",
            Observations = "Contrôle réglementaire simulé — aucune intégration BCEAO/FINEX réelle.",
        };
    }
}
