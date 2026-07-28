using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.CBS.Integration.Features.VerifierSignatureClient;

public record VerifierSignatureClientQuery(string NumCompte, ModeVerificationSignature? Mode) : IRequest<SignatureVerificationResult>;
