using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Admin.API.Mapping;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;

namespace PDOE.Admin.API.Features.ListParametrage;

public class ListParametrageHandler(PdoeDbContext db) : IRequestHandler<ListParametrageQuery, List<ParametreMetierResponse>>
{
    public async Task<List<ParametreMetierResponse>> Handle(ListParametrageQuery request, CancellationToken cancellationToken)
    {
        var parametres = await db.ParametresMetier.OrderBy(p => p.Cle).ToListAsync(cancellationToken);
        return parametres.Select(p => p.ToResponse()).ToList();
    }
}
