using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Workflow.API.Features.ValiderEtape;

public record ValiderEtapeCommand(int DossierId, ValiderEtapeRequest Request) : IRequest<WorkflowTransitionResponse>;
