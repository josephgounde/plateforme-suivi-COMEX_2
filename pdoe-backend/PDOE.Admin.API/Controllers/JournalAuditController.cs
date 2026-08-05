using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDOE.Admin.API.Features.ListJournalAudit;
using PDOE.Api.Contracts;

namespace PDOE.Admin.API.Controllers;

// Admin DSIRI délibérément exclu (séparation des tâches) — cf. description du tag JournalAudit dans l'OpenAPI.
[ApiController]
[Route("journal-audit")]
[Authorize(Policy = "SuperAdmin")]
public class JournalAuditController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<JournalAuditEntry>>> ListJournalAudit(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListJournalAuditQuery(), cancellationToken);
        return Ok(result);
    }
}
