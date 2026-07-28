using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Workflow.API.Features.CreerEtapeConfig;

public record CreerEtapeConfigCommand(EtapeWorkflowConfigCreateRequest Request) : IRequest<EtapeWorkflowConfig>;
