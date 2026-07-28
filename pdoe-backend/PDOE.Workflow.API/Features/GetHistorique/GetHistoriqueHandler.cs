using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Shared.Kernel.Common;
using PDOE.Workflow.API.Mapping;

namespace PDOE.Workflow.API.Features.GetHistorique;

public class GetHistoriqueHandler(PdoeDbContext db) : IRequestHandler<GetHistoriqueQuery, List<EtapeWorkflowResponse>>
{
    public async Task<List<EtapeWorkflowResponse>> Handle(GetHistoriqueQuery request, CancellationToken cancellationToken)
    {
        var dossierExiste = await db.Dossiers.AnyAsync(d => d.DossierId == request.DossierId, cancellationToken);
        if (!dossierExiste)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        var etapes = await db.EtapesWorkflow
            .Where(e => e.DossierId == request.DossierId)
            .OrderBy(e => e.DateAction)
            .ToListAsync(cancellationToken);

        return etapes.Select(e => e.ToResponse()).ToList();
    }
}
