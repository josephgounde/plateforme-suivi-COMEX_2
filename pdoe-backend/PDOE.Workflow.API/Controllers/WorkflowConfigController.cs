using MediatR;
using Microsoft.AspNetCore.Mvc;
using PDOE.Api.Contracts;
using PDOE.Workflow.API.Features.CreerEtapeConfig;
using PDOE.Workflow.API.Features.ListEtapesConfig;
using PDOE.Workflow.API.Features.ModifierEtapeConfig;
using PDOE.Workflow.API.Features.ReordonnerEtapesConfig;

namespace PDOE.Workflow.API.Controllers;

[ApiController]
[Route("workflow-config")]
public class WorkflowConfigController(IMediator mediator) : ControllerBase
{
    [HttpGet("etapes")]
    public async Task<ActionResult<List<EtapeWorkflowConfig>>> ListEtapes(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListEtapesConfigQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("etapes")]
    public async Task<ActionResult<EtapeWorkflowConfig>> CreerEtape(
        [FromBody] EtapeWorkflowConfigCreateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreerEtapeConfigCommand(request), cancellationToken);
        return StatusCode(201, result);
    }

    [HttpPatch("etapes/reordonner")]
    public async Task<ActionResult<List<EtapeWorkflowConfig>>> ReordonnerEtapes(
        [FromBody] ReordonnerEtapesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ReordonnerEtapesConfigCommand(request), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("etapes/{code}")]
    public async Task<ActionResult<EtapeWorkflowConfig>> ModifierEtape(
        string code,
        [FromBody] EtapeWorkflowConfigUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ModifierEtapeConfigCommand(code, request), cancellationToken);
        return Ok(result);
    }
}
