using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Workflow.API.Features.ControleLcbft;

/// <summary>Stub — aucune intégration LCB-FT réelle n'existe. Retourne systématiquement conforme.</summary>
public class ControleLcbftHandler(PdoeDbContext db) : IRequestHandler<ControleLcbftCommand, ControleLcbftResult>
{
    public async Task<ControleLcbftResult> Handle(ControleLcbftCommand command, CancellationToken cancellationToken)
    {
        var dossierExiste = await db.Dossiers.AnyAsync(d => d.DossierId == command.DossierId, cancellationToken);
        if (!dossierExiste)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        return new ControleLcbftResult(true, "Contrôle LCB-FT simulé — aucune intégration réelle.");
    }
}
