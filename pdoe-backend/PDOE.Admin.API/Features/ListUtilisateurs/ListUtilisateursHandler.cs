using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Admin.API.Mapping;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;

namespace PDOE.Admin.API.Features.ListUtilisateurs;

public class ListUtilisateursHandler(PdoeDbContext db) : IRequestHandler<ListUtilisateursQuery, List<UtilisateurResponse>>
{
    public async Task<List<UtilisateurResponse>> Handle(ListUtilisateursQuery query, CancellationToken cancellationToken)
    {
        var utilisateurs = await db.Utilisateurs.OrderBy(u => u.LoginAD).ToListAsync(cancellationToken);
        return utilisateurs.Select(u => u.ToResponse()).ToList();
    }
}
