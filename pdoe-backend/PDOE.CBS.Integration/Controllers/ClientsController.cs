using MediatR;
using Microsoft.AspNetCore.Mvc;
using PDOE.Api.Contracts;
using PDOE.CBS.Integration.Features.ObtenirSoldeClient;
using PDOE.CBS.Integration.Features.ValiderSignatureVisuelle;
using PDOE.CBS.Integration.Features.VerifierSignatureClient;

namespace PDOE.CBS.Integration.Controllers;

[ApiController]
[Route("clients")]
public class ClientsController(IMediator mediator) : ControllerBase
{
    [HttpGet("{numCompte}/verifier-signature")]
    public async Task<ActionResult<SignatureVerificationResult>> VerifierSignature(
        string numCompte,
        [FromQuery] ModeVerificationSignature? mode,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new VerifierSignatureClientQuery(numCompte, mode), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{numCompte}/valider-signature-visuelle")]
    public async Task<ActionResult<object>> ValiderSignatureVisuelle(
        string numCompte,
        [FromBody] ValidationVisuelleRequest request,
        CancellationToken cancellationToken)
    {
        var signatureValidee = await mediator.Send(
            new ValiderSignatureVisuelleCommand(numCompte, request.InitialesAgent), cancellationToken);
        return Ok(new { signatureValidee });
    }

    [HttpGet("{numCompte}/solde")]
    public async Task<ActionResult<SoldeClientResult>> ObtenirSolde(
        string numCompte,
        [FromQuery] int dossierId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ObtenirSoldeClientQuery(numCompte, dossierId), cancellationToken);
        return Ok(result);
    }
}
