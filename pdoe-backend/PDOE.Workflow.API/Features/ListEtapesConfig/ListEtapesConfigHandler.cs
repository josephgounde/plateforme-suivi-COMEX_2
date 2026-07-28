using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Workflow.API.Mapping;

namespace PDOE.Workflow.API.Features.ListEtapesConfig;

public class ListEtapesConfigHandler(PdoeDbContext db) : IRequestHandler<ListEtapesConfigQuery, List<EtapeWorkflowConfig>>
{
    public async Task<List<EtapeWorkflowConfig>> Handle(ListEtapesConfigQuery query, CancellationToken cancellationToken)
    {
        var etapes = await db.WorkflowEtapes
            .OrderBy(e => e.Ordre)
            .ToListAsync(cancellationToken);

        return etapes.Select(e => e.ToResponse()).ToList();
    }
}
