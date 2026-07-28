using MediatR;
using PDOE.Api.Contracts;
using ChecklistItemConfigResponse = PDOE.Api.Contracts.ChecklistItemConfig;

namespace PDOE.Apurement.API.Features.CreerChecklistItemConfig;

public record CreerChecklistItemConfigCommand(ChecklistItemConfigCreateRequest Request) : IRequest<ChecklistItemConfigResponse>;
