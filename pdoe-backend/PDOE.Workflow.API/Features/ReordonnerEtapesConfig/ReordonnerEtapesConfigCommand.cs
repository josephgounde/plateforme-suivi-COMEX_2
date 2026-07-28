using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Workflow.API.Features.ReordonnerEtapesConfig;

public record ReordonnerEtapesConfigCommand(ReordonnerEtapesRequest Request) : IRequest<List<EtapeWorkflowConfig>>;
