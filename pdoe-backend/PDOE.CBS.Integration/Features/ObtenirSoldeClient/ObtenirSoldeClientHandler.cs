using MediatR;
using PDOE.Api.Contracts;
using PDOE.Shared.Kernel.Common;

namespace PDOE.CBS.Integration.Features.ObtenirSoldeClient;

/// Mocké en attendant ABS2000, même compte de test CI99999 (introuvable) que VerifierSignatureClientHandler.
public class ObtenirSoldeClientHandler : IRequestHandler<ObtenirSoldeClientQuery, SoldeClientResult>
{
    public Task<SoldeClientResult> Handle(ObtenirSoldeClientQuery request, CancellationToken cancellationToken)
    {
        if (request.NumCompte == "CI99999")
        {
            throw new DomainException(404, ErrorResponseCode.COMPTE_INTROUVABLE,
                "Numéro de compte introuvable dans ABS2000");
        }

        return Task.FromResult(new SoldeClientResult
        {
            NumCompte = request.NumCompte,
            SoldeDisponible = 125_000_000,
            Devise = "XOF",
            Suffisant = true,
            DateConsultation = DateTimeOffset.UtcNow
        });
    }
}
