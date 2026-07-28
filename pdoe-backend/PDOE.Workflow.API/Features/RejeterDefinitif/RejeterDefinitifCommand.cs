using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Workflow.API.Features.RejeterDefinitif;

// DTO local (pas de $ref dans l'OpenAPI) pour éviter le nommage fragile "BodyN" que NSwag génère pour les schémas inline.
public record RejeterDefinitifRequest(string Motif);

public record RejeterDefinitifCommand(int DossierId, RejeterDefinitifRequest Request) : IRequest<WorkflowTransitionResponse>;
