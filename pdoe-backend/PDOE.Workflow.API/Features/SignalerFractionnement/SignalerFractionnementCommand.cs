using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Workflow.API.Features.SignalerFractionnement;

// DTO local (pas de $ref dans l'OpenAPI), même situation que RejeterDefinitifRequest — évite le nommage fragile "BodyN" de NSwag.
public record SignalerFractionnementRequest(string Motif);

public record SignalerFractionnementCommand(int DossierId, SignalerFractionnementRequest Request) : IRequest<WorkflowTransitionResponse>;
