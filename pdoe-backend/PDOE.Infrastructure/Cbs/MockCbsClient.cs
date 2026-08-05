using PDOE.Api.Contracts;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Infrastructure.Cbs;

/// Version dev/test, aucun accès réseau — mêmes cotations et comptes de test que le code CBS.Integration
/// d'origine (CI99999 introuvable, CI00000 signature absente), déplacés ici tels quels lors de l'introduction
/// d'ICbsClient.
public class MockCbsClient : ICbsClient
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

    // Signature svg de démonstration (même image que MockDataService.mockVerifierSignature côté frontend).
    private static readonly byte[] SignatureSvgFactice = Convert.FromBase64String(
        "PHN2ZyB3aWR0aD0iMjAwIiBoZWlnaHQ9IjgwIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciPjxwYXRoIGQ9Ik0xMCA2MCBRIDUwIDEwIDkwIDUwIFEgMTMwIDkwIDE3MCA0MCIgc3Ryb2tlPSIjMUExQTFBIiBzdHJva2Utd2lkdGg9IjIiIGZpbGw9Im5vbmUiLz48L3N2Zz4=");

    public Task<TauxChangeResult> ObtenirTauxChangeAsync(string devise, string versDevise, CancellationToken cancellationToken)
    {
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

    public Task<SoldeClientResult> ObtenirSoldeClientAsync(string numCompte, CancellationToken cancellationToken)
    {
        if (numCompte == "CI99999")
        {
            throw new DomainException(404, ErrorResponseCode.COMPTE_INTROUVABLE,
                "Numéro de compte introuvable dans ABS2000");
        }

        return Task.FromResult(new SoldeClientResult
        {
            NumCompte = numCompte,
            SoldeDisponible = 125_000_000,
            Devise = "XOF",
            Suffisant = true,
            DateConsultation = DateTimeOffset.UtcNow
        });
    }

    public Task<SignatureVerificationResult> VerifierSignatureAsync(string numCompte, ModeVerificationSignature mode, CancellationToken cancellationToken)
    {
        if (numCompte == "CI99999")
        {
            throw new DomainException(404, ErrorResponseCode.COMPTE_INTROUVABLE,
                "Numéro de compte introuvable dans ABS2000");
        }

        if (numCompte == "CI00000")
        {
            throw new DomainException(422, ErrorResponseCode.SIGNATURE_INVALIDE,
                "Signature client absente dans ABS2000");
        }

        return Task.FromResult(new SignatureVerificationResult
        {
            Trouve = true,
            SignatureExistante = true,
            NomClient = "CLIENT TEST SA",
            TypeCompte = "COURANT",
            DateSignature = new DateTimeOffset(2024, 3, 15, 0, 0, 0, TimeSpan.Zero),
            ImageSignature = mode != ModeVerificationSignature.AUTOMATIQUE ? SignatureSvgFactice : null,
            ModeVerification = mode,
            NifClient = "9601990K",
            AdressePostaleClient = "01 BP 1234 Abidjan 01",
            AdresseGeographiqueClient = "Cocody Riviera, Rue des Jardins",
            TelephoneClient = "+225 07 00 00 00 00",
            QualiteResidence = QualiteResidence.RESIDENT,
            DateOuvertureCompte = new DateTimeOffset(2020, 5, 12, 0, 0, 0, TimeSpan.Zero),
            AnneeExerciceCompte = DateTime.UtcNow.Year
        });
    }

    public Task<bool> ValiderSignatureVisuelleAsync(string numCompte, string initialesAgent, CancellationToken cancellationToken)
        => Task.FromResult(true);
}
