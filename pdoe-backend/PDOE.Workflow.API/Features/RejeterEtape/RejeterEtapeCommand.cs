using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Workflow.API.Features.RejeterEtape;

public record RejeterEtapeCommand(int DossierId, RejeterEtapeRequest Request) : IRequest<WorkflowTransitionResponse>;
