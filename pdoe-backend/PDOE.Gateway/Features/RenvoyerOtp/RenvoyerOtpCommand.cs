using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Gateway.Features.RenvoyerOtp;

public record RenvoyerOtpCommand(RenvoyerOtpRequest Request) : IRequest<OtpChallengeResponse>;
