using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Dossiers.API.Mapping;
using PDOE.Infrastructure;

namespace PDOE.Dossiers.API.Features.ListDossiers;

public class ListDossiersHandler(PdoeDbContext db) : IRequestHandler<ListDossiersQuery, DossierListResponse>
{
    public async Task<DossierListResponse> Handle(ListDossiersQuery request, CancellationToken cancellationToken)
    {
        var query = db.Dossiers.Include(d => d.EtapesWorkflow).AsQueryable();

        if (request.Statut is { } statut)
        {
            var statutValue = statut.ToString();
            query = query.Where(d => d.StatutElectronique == statutValue);
        }

        if (request.TypeOperation is { } typeOperation)
        {
            var typeOperationValue = typeOperation.ToString();
            query = query.Where(d => d.TypeOperation == typeOperationValue);
        }

        if (!string.IsNullOrWhiteSpace(request.NumCompte))
            query = query.Where(d => d.NumCompte == request.NumCompte);

        if (request.DateDebutCreation is { } dateDebut)
        {
            var from = dateDebut.ToDateTime(TimeOnly.MinValue);
            query = query.Where(d => d.CreatedAt >= from);
        }

        if (request.DateFinCreation is { } dateFin)
        {
            var to = dateFin.ToDateTime(TimeOnly.MaxValue);
            query = query.Where(d => d.CreatedAt <= to);
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new DossierListResponse
        {
            Items = items.Select(d => d.ToResponse()).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}
