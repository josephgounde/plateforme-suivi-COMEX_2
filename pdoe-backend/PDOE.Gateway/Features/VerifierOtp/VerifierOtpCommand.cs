using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Gateway.Features.VerifierOtp;

public record VerifierOtpCommand(VerifierOtpRequest Request) : IRequest<SessionResponse>;
