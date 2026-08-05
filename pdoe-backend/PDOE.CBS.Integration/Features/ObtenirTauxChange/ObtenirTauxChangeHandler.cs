using MediatR;
using PDOE.Api.Contracts;
using PDOE.Infrastructure.Cbs;

namespace PDOE.CBS.Integration.Features.ObtenirTauxChange;

public class ObtenirTauxChangeHandler(ICbsClient cbs) : IRequestHandler<ObtenirTauxChangeQuery, TauxChangeResult>
{
    public Task<TauxChangeResult> Handle(ObtenirTauxChangeQuery request, CancellationToken cancellationToken)
    {
        var devise = (request.Devise ?? "XOF").ToUpperInvariant();
        var versDevise = (request.VersDevise ?? "XOF").ToUpperInvariant();

        return cbs.ObtenirTauxChangeAsync(devise, versDevise, cancellationToken);
    }
}
