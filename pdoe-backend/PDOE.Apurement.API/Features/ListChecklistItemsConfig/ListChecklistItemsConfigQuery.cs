using MediatR;
using ChecklistItemConfigResponse = PDOE.Api.Contracts.ChecklistItemConfig;

namespace PDOE.Apurement.API.Features.ListChecklistItemsConfig;

public record ListChecklistItemsConfigQuery : IRequest<List<ChecklistItemConfigResponse>>;
