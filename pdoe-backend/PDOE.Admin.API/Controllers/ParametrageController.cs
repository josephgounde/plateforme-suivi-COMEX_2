using MediatR;
using Microsoft.AspNetCore.Mvc;
using PDOE.Admin.API.Features.GetParametre;
using PDOE.Admin.API.Features.ListParametrage;
using PDOE.Admin.API.Features.UpdateParametre;
using PDOE.Api.Contracts;

namespace PDOE.Admin.API.Controllers;

[ApiController]
[Route("parametrage")]
public class ParametrageController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ParametreMetierResponse>>> ListParametrage(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListParametrageQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{cle}")]
    public async Task<ActionResult<ParametreMetierResponse>> GetParametre(string cle, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetParametreQuery(cle), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{cle}")]
    public async Task<ActionResult<ParametreMetierResponse>> UpdateParametre(
        string cle,
        [FromBody] UpdateParametreRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateParametreCommand(cle, request), cancellationToken);
        return Ok(result);
    }
}
