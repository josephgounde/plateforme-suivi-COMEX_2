using MediatR;
using PDOE.Api.Contracts;
using ChecklistItemConfigResponse = PDOE.Api.Contracts.ChecklistItemConfig;

namespace PDOE.Apurement.API.Features.ModifierChecklistItemConfig;

public record ModifierChecklistItemConfigCommand(int ChecklistItemId, ChecklistItemConfigUpdateRequest Request) : IRequest<ChecklistItemConfigResponse>;
