using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Workflow.API.Features.ModifierEtapeConfig;

public record ModifierEtapeConfigCommand(string Code, EtapeWorkflowConfigUpdateRequest Request) : IRequest<EtapeWorkflowConfig>;
