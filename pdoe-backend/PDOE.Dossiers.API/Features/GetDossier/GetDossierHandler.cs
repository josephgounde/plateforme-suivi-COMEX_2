using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Dossiers.API.Mapping;
using PDOE.Infrastructure;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Dossiers.API.Features.GetDossier;

public class GetDossierHandler(PdoeDbContext db) : IRequestHandler<GetDossierQuery, DossierDetailResponse>
{
    public async Task<DossierDetailResponse> Handle(GetDossierQuery request, CancellationToken cancellationToken)
    {
        var dossier = await db.Dossiers
            .Include(d => d.Documents)
            .Include(d => d.EtapesWorkflow)
            .Include(d => d.PaiementsPartiels)
            .Include(d => d.Alertes)
            .FirstOrDefaultAsync(d => d.DossierId == request.DossierId, cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        return dossier.ToDetailResponse();
    }
}
