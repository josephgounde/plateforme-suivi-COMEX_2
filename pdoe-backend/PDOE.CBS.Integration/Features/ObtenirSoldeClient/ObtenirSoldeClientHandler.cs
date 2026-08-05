using MediatR;
using PDOE.Api.Contracts;
using PDOE.Infrastructure.Cbs;

namespace PDOE.CBS.Integration.Features.ObtenirSoldeClient;

public class ObtenirSoldeClientHandler(ICbsClient cbs) : IRequestHandler<ObtenirSoldeClientQuery, SoldeClientResult>
{
    public Task<SoldeClientResult> Handle(ObtenirSoldeClientQuery request, CancellationToken cancellationToken)
        => cbs.ObtenirSoldeClientAsync(request.NumCompte, cancellationToken);
}
