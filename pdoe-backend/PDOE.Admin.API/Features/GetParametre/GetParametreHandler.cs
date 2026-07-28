using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Admin.API.Mapping;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Admin.API.Features.GetParametre;

public class GetParametreHandler(PdoeDbContext db) : IRequestHandler<GetParametreQuery, ParametreMetierResponse>
{
    public async Task<ParametreMetierResponse> Handle(GetParametreQuery request, CancellationToken cancellationToken)
    {
        var parametre = await db.ParametresMetier.FirstOrDefaultAsync(p => p.Cle == request.Cle, cancellationToken);

        if (parametre is null)
            throw new DomainException(404, ErrorResponseCode.PARAMETRE_INTROUVABLE, "Paramètre introuvable.");

        return parametre.ToResponse();
    }
}
