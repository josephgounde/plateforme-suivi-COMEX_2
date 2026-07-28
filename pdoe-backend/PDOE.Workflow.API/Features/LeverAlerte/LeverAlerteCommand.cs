using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Workflow.API.Features.LeverAlerte;

public record LeverAlerteCommand(int DossierId) : IRequest<WorkflowTransitionResponse>;
