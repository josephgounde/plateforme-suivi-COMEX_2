using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Apurement.API.Mapping;
using PDOE.Infrastructure;
using ChecklistItemConfigResponse = PDOE.Api.Contracts.ChecklistItemConfig;

namespace PDOE.Apurement.API.Features.ListChecklistItemsConfig;

public class ListChecklistItemsConfigHandler(PdoeDbContext db) : IRequestHandler<ListChecklistItemsConfigQuery, List<ChecklistItemConfigResponse>>
{
    public async Task<List<ChecklistItemConfigResponse>> Handle(ListChecklistItemsConfigQuery request, CancellationToken cancellationToken)
    {
        var items = await db.ChecklistItemsConfig.OrderBy(c => c.Ordre).ToListAsync(cancellationToken);
        return items.Select(c => c.ToResponse()).ToList();
    }
}
