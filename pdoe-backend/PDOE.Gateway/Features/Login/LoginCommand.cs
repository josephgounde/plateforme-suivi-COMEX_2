using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Gateway.Features.Login;

public record LoginCommand(LoginRequest Request) : IRequest<OtpChallengeResponse>;
