using MediatR;

namespace PDOE.Workflow.API.Features.ControleLcbft;

// DTOs locaux (pas de $ref dans l'OpenAPI) pour éviter le nommage fragile "BodyN"/"ResponseN" que NSwag génère pour les schémas anonymes.
public record ControleLcbftRequest(string? AgentLogin);

public record ControleLcbftResult(bool LcbftConforme, string? Observations);

public record ControleLcbftCommand(int DossierId, ControleLcbftRequest Request) : IRequest<ControleLcbftResult>;
