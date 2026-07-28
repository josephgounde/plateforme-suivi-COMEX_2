using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.CBS.Integration.Features.ObtenirTauxChange;

/// Mocké en attendant un vrai fournisseur de cours. Devise absente de la table = taux 1 (mieux que deviner).
public class ObtenirTauxChangeHandler : IRequestHandler<ObtenirTauxChangeQuery, TauxChangeResult>
{
    private static readonly IReadOnlyDictionary<string, double> TauxChangeBase = new Dictionary<string, double>
    {
        ["EUR"] = 655.957, // parité fixe FCFA/EUR
        ["USD"] = 605.2,
        ["GBP"] = 765.4,
        ["CHF"] = 690.1,
        ["CAD"] = 445.0,
        ["CNY"] = 83.5,
        ["XOF"] = 1,
    };

    public Task<TauxChangeResult> Handle(ObtenirTauxChangeQuery request, CancellationToken cancellationToken)
    {
        var devise = (request.Devise ?? "XOF").ToUpperInvariant();
        var versDevise = (request.VersDevise ?? "XOF").ToUpperInvariant();

        var baseSource = TauxChangeBase.GetValueOrDefault(devise, 1);
        var baseCible = TauxChangeBase.GetValueOrDefault(versDevise, 1);
        var bruit = 1 + (Random.Shared.NextDouble() - 0.5) * 0.006; // ±0,3%

        return Task.FromResult(new TauxChangeResult
        {
            Devise = devise,
            Taux = Math.Round(baseSource / baseCible * bruit, 6),
            DeviseCotation = versDevise,
            DateCotation = DateTime.UtcNow,
        });
    }
}
