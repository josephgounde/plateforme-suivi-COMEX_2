using MediatR;
using PDOE.Api.Contracts;
using ChecklistItemConfigResponse = PDOE.Api.Contracts.ChecklistItemConfig;

namespace PDOE.Apurement.API.Features.ReordonnerChecklistItemsConfig;

public record ReordonnerChecklistItemsConfigCommand(ReordonnerChecklistItemsRequest Request) : IRequest<List<ChecklistItemConfigResponse>>;
