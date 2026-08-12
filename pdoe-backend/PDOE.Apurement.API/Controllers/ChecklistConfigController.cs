using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDOE.Api.Contracts;
using PDOE.Apurement.API.Features.CreerChecklistItemConfig;
using PDOE.Apurement.API.Features.ListChecklistItemsConfig;
using PDOE.Apurement.API.Features.ModifierChecklistItemConfig;
using PDOE.Apurement.API.Features.ReordonnerChecklistItemsConfig;
using ChecklistItemConfigResponse = PDOE.Api.Contracts.ChecklistItemConfig;

namespace PDOE.Apurement.API.Controllers;

[ApiController]
[Route("checklist-config")]
public class ChecklistConfigController(IMediator mediator) : ControllerBase
{
    // Pas de [Authorize] ici : lecture ouverte à tout utilisateur authentifié (FallbackPolicy, cf. Program.cs) —
    // consommé par ApurementDetailComponent pour afficher la checklist, pas seulement par l'écran d'admin.
    [HttpGet("items")]
    public async Task<ActionResult<List<ChecklistItemConfigResponse>>> ListItems(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListChecklistItemsConfigQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("items")]
    [Authorize(Policy = "AdminDsiri")]
    public async Task<ActionResult<ChecklistItemConfigResponse>> CreerItem(
        [FromBody] ChecklistItemConfigCreateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreerChecklistItemConfigCommand(request), cancellationToken);
        return StatusCode(201, result);
    }

    [HttpPatch("items/reordonner")]
    [Authorize(Policy = "AdminDsiri")]
    public async Task<ActionResult<List<ChecklistItemConfigResponse>>> ReordonnerItems(
        [FromBody] ReordonnerChecklistItemsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ReordonnerChecklistItemsConfigCommand(request), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("items/{checklistItemId:int}")]
    [Authorize(Policy = "AdminDsiri")]
    public async Task<ActionResult<ChecklistItemConfigResponse>> ModifierItem(
        int checklistItemId,
        [FromBody] ChecklistItemConfigUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ModifierChecklistItemConfigCommand(checklistItemId, request), cancellationToken);
        return Ok(result);
    }
}
