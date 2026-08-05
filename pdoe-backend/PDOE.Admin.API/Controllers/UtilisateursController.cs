using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDOE.Admin.API.Features.CreerUtilisateur;
using PDOE.Admin.API.Features.ListUtilisateurs;
using PDOE.Admin.API.Features.ModifierUtilisateur;
using PDOE.Api.Contracts;

namespace PDOE.Admin.API.Controllers;

[ApiController]
[Route("utilisateurs")]
[Authorize(Policy = "AdminDsiri")]
public class UtilisateursController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UtilisateurResponse>>> ListUtilisateurs(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListUtilisateursQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<UtilisateurResponse>> CreerUtilisateur([FromBody] CreerUtilisateurRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreerUtilisateurCommand(request), cancellationToken);
        return StatusCode(201, result);
    }

    [HttpPatch("{utilisateurId}")]
    public async Task<ActionResult<UtilisateurResponse>> ModifierUtilisateur(
        int utilisateurId,
        [FromBody] ModifierUtilisateurRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ModifierUtilisateurCommand(utilisateurId, request), cancellationToken);
        return Ok(result);
    }
}
