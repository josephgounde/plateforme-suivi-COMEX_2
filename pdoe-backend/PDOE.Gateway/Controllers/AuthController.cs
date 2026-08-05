using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDOE.Api.Contracts;
using PDOE.Gateway.Features.Login;
using PDOE.Gateway.Features.Logout;
using PDOE.Gateway.Features.RenvoyerOtp;
using PDOE.Gateway.Features.VerifierOtp;

namespace PDOE.Gateway.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<OtpChallengeResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new LoginCommand(request), cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("otp/verifier")]
    public async Task<ActionResult<SessionResponse>> VerifierOtp([FromBody] VerifierOtpRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new VerifierOtpCommand(request), cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("otp/renvoyer")]
    public async Task<ActionResult<OtpChallengeResponse>> RenvoyerOtp([FromBody] RenvoyerOtpRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RenvoyerOtpCommand(request), cancellationToken);
        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await mediator.Send(new LogoutCommand(), cancellationToken);
        return Ok();
    }
}
