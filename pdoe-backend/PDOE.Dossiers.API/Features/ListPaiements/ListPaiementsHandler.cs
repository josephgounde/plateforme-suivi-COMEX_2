using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Dossiers.API.Mapping;
using PDOE.Infrastructure;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Dossiers.API.Features.ListPaiements;

public class ListPaiementsHandler(PdoeDbContext db) : IRequestHandler<ListPaiementsQuery, PaiementListResponse>
{
    public async Task<PaiementListResponse> Handle(ListPaiementsQuery query, CancellationToken cancellationToken)
    {
        var dossier = await db.Dossiers
            .Include(d => d.PaiementsPartiels)
            .FirstOrDefaultAsync(d => d.DossierId == query.DossierId, cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        var paiements = dossier.PaiementsPartiels.OrderBy(p => p.CreatedAt).ToList();

        return new PaiementListResponse
        {
            Items = paiements.Select(p => p.ToResponse()).ToList(),
            MontantInitial = (double)dossier.Montant,
            TotalPaye = (double)paiements.Sum(p => p.MontantPaiement),
            SoldeRestant = (double)(dossier.SoldeRestantApurement ?? dossier.Montant),
            NbPaiements = paiements.Count,
        };
    }
}
