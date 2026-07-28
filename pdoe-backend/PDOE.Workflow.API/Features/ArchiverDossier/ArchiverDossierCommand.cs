using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Workflow.API.Features.ArchiverDossier;

public record ArchiverDossierCommand(int DossierId) : IRequest<WorkflowTransitionResponse>;
