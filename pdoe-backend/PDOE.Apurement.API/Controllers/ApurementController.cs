using MediatR;
using Microsoft.AspNetCore.Mvc;
using PDOE.Api.Contracts;
using PDOE.Apurement.API.Features.DeclarerDepassement;
using PDOE.Apurement.API.Features.GetAlertes;
using PDOE.Apurement.API.Features.ValiderChecklist;

namespace PDOE.Apurement.API.Controllers;

[ApiController]
[Route("apurement")]
public class ApurementController(IMediator mediator) : ControllerBase
{
    [HttpPost("{dossierId:int}/checklist")]
    public async Task<ActionResult<DossierResponse>> ValiderChecklist(
        int dossierId,
        [FromBody] ChecklistRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ValiderChecklistCommand(dossierId, request), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{dossierId:int}/declarer-depassement")]
    public async Task<ActionResult<DossierResponse>> DeclarerDepassement(
        int dossierId,
        [FromBody] DepassementRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeclarerDepassementCommand(dossierId, request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{dossierId:int}/alertes")]
    public async Task<ActionResult<List<AlerteApurementResponse>>> GetAlertes(int dossierId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAlertesQuery(dossierId), cancellationToken);
        return Ok(result);
    }
}
