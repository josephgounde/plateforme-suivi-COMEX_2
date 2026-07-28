using MediatR;
using Microsoft.AspNetCore.Mvc;
using PDOE.Api.Contracts;
using PDOE.CBS.Integration.Features.ObtenirTauxChange;

namespace PDOE.CBS.Integration.Controllers;

[ApiController]
[Route("taux-change")]
public class TauxChangeController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TauxChangeResult>> ObtenirTauxChange(
        [FromQuery] string? devise,
        [FromQuery] string? versDevise,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ObtenirTauxChangeQuery(devise, versDevise), cancellationToken);
        return Ok(result);
    }
}
