using MediatR;
using Microsoft.AspNetCore.Mvc;
using PDOE.Admin.API.Features.ListJournalAudit;
using PDOE.Api.Contracts;

namespace PDOE.Admin.API.Controllers;

[ApiController]
[Route("journal-audit")]
public class JournalAuditController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<JournalAuditEntry>>> ListJournalAudit(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListJournalAuditQuery(), cancellationToken);
        return Ok(result);
    }
}
