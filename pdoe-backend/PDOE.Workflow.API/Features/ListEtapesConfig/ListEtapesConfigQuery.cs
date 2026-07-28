using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Workflow.API.Features.ListEtapesConfig;

public record ListEtapesConfigQuery : IRequest<List<EtapeWorkflowConfig>>;
