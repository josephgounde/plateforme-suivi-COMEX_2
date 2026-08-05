using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Cbs;

namespace PDOE.CBS.Integration.Features.VerifierSignatureClient;

public class VerifierSignatureClientHandler(PdoeDbContext db, ICbsClient cbs) : IRequestHandler<VerifierSignatureClientQuery, SignatureVerificationResult>
{
    public async Task<SignatureVerificationResult> Handle(VerifierSignatureClientQuery request, CancellationToken cancellationToken)
    {
        var mode = request.Mode ?? await ResoudreModeConfigure(cancellationToken);
        return await cbs.VerifierSignatureAsync(request.NumCompte, mode, cancellationToken);
    }

    // Réglage propre à PDOE (pas à ABS2000) : combien de preuve visuelle exiger de l'agent avant de continuer.
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
}
