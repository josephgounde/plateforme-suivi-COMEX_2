using MediatR;

namespace PDOE.CBS.Integration.Features.ValiderSignatureVisuelle;

/// Enregistre la confirmation visuelle (modes VISUEL/LES_DEUX). Mocké comme VerifierSignatureClientHandler.
public class ValiderSignatureVisuelleHandler : IRequestHandler<ValiderSignatureVisuelleCommand, bool>
{
    public Task<bool> Handle(ValiderSignatureVisuelleCommand request, CancellationToken cancellationToken)
        => Task.FromResult(true);
}
