using MediatR;
using PDOE.Infrastructure.Cbs;

namespace PDOE.CBS.Integration.Features.ValiderSignatureVisuelle;

public class ValiderSignatureVisuelleHandler(ICbsClient cbs) : IRequestHandler<ValiderSignatureVisuelleCommand, bool>
{
    public Task<bool> Handle(ValiderSignatureVisuelleCommand request, CancellationToken cancellationToken)
        => cbs.ValiderSignatureVisuelleAsync(request.NumCompte, request.InitialesAgent, cancellationToken);
}
