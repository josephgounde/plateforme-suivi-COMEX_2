using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Shared.Kernel.Common;

namespace PDOE.CBS.Integration.Features.VerifierSignatureClient;

/// Mocké en attendant ABS2000. Comptes de test : CI99999 (introuvable), CI00000 (signature absente).
/// Renvoie aussi les infos client pour préremplir "Compte client" côté frontend — CodeBanque exclu, c'est nous pas le client.
public class VerifierSignatureClientHandler(PdoeDbContext db) : IRequestHandler<VerifierSignatureClientQuery, SignatureVerificationResult>
{
    public async Task<SignatureVerificationResult> Handle(VerifierSignatureClientQuery request, CancellationToken cancellationToken)
    {
        if (request.NumCompte == "CI99999")
        {
            throw new DomainException(404, ErrorResponseCode.COMPTE_INTROUVABLE,
                "Numéro de compte introuvable dans ABS2000");
        }

        var mode = request.Mode ?? await ResoudreModeConfigure(cancellationToken);

        if (request.NumCompte == "CI00000")
        {
            throw new DomainException(422, ErrorResponseCode.SIGNATURE_INVALIDE,
                "Signature client absente dans ABS2000");
        }

        return new SignatureVerificationResult
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
        };
    }

    private async Task<ModeVerificationSignature> ResoudreModeConfigure(CancellationToken cancellationToken)
    {
        var valeur = await db.ParametresMetier
            .Where(p => p.Cle == "MODE_VERIFICATION_SIGNATURE")
            .Select(p => p.Valeur)
            .FirstOrDefaultAsync(cancellationToken);

        return Enum.TryParse<ModeVerificationSignature>(valeur, ignoreCase: true, out var parsed)
            ? parsed
            : ModeVerificationSignature.AUTOMATIQUE;
    }

    // Signature svg de démonstration (même image que MockDataService.mockVerifierSignature côté frontend).
    private static readonly byte[] SignatureSvgFactice = Convert.FromBase64String(
        "PHN2ZyB3aWR0aD0iMjAwIiBoZWlnaHQ9IjgwIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciPjxwYXRoIGQ9Ik0xMCA2MCBRIDUwIDEwIDkwIDUwIFEgMTMwIDkwIDE3MCA0MCIgc3Ryb2tlPSIjMUExQTFBIiBzdHJva2Utd2lkdGg9IjIiIGZpbGw9Im5vbmUiLz48L3N2Zz4=");
}
