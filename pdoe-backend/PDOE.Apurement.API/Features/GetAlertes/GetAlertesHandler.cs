using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Apurement.API.Mapping;
using PDOE.Infrastructure;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Apurement.API.Features.GetAlertes;

public class GetAlertesHandler(PdoeDbContext db) : IRequestHandler<GetAlertesQuery, List<AlerteApurementResponse>>
{
    public async Task<List<AlerteApurementResponse>> Handle(GetAlertesQuery query, CancellationToken cancellationToken)
    {
        var apurementComplet = await db.Dossiers
            .Where(d => d.DossierId == query.DossierId)
            .Select(d => (bool?)d.ApurementComplet)
            .SingleOrDefaultAsync(cancellationToken);

        if (apurementComplet is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        // Dossier apuré : les alertes restent en base (historique) mais on arrête de les exposer, sinon J-14 réapparaît indéfiniment à l'écran.
        if (apurementComplet == true)
            return [];

        var alertes = await db.AlertesApurement
            .Where(a => a.DossierId == query.DossierId)
            .OrderBy(a => a.DateAlerte)
            .ToListAsync(cancellationToken);

        return alertes.Select(a => a.ToResponse()).ToList();
    }
}
