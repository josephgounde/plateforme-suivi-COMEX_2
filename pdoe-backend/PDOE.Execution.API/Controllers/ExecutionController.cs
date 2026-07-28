using MediatR;
using Microsoft.AspNetCore.Mvc;
using PDOE.Api.Contracts;
using PDOE.Execution.API.Features.BasculerExecution;
using PDOE.Execution.API.Features.DeclarerExecution;
using PDOE.Execution.API.Features.GetExecution;

namespace PDOE.Execution.API.Controllers;

[ApiController]
[Route("execution")]
public class ExecutionController(IMediator mediator) : ControllerBase
{
    [HttpPost("{dossierId:int}/basculer")]
    public async Task<ActionResult<DossierResponse>> Basculer(int dossierId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new BasculerExecutionCommand(dossierId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{dossierId:int}/declarer")]
    public async Task<ActionResult<ExecutionDeclarationResponse>> Declarer(
        int dossierId,
        [FromBody] DeclarerExecutionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeclarerExecutionCommand(dossierId, request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{dossierId:int}")]
    public async Task<ActionResult<ExecutionDetailResponse>> GetExecution(int dossierId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetExecutionQuery(dossierId), cancellationToken);
        return Ok(result);
    }
}
