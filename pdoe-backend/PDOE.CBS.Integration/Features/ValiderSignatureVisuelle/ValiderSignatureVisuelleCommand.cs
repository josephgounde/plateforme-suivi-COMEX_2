using MediatR;

namespace PDOE.CBS.Integration.Features.ValiderSignatureVisuelle;

public record ValiderSignatureVisuelleCommand(string NumCompte, string InitialesAgent) : IRequest<bool>;
