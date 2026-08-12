using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Cbs;
using PDOE.Shared.Kernel.Common;

namespace PDOE.CBS.Integration.Features.ObtenirSoldeClient;

public class ObtenirSoldeClientHandler(ICbsClient cbs, PdoeDbContext db) : IRequestHandler<ObtenirSoldeClientQuery, SoldeClientResult>
{
    public async Task<SoldeClientResult> Handle(ObtenirSoldeClientQuery request, CancellationToken cancellationToken)
    {
        var dossier = await db.Dossiers
            .Where(d => d.DossierId == request.DossierId)
            .Select(d => new { d.Montant, d.Devise })
            .FirstOrDefaultAsync(cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        var resultat = await cbs.ObtenirSoldeClientAsync(request.NumCompte, cancellationToken);

        // ABS2000 ne renvoie que le solde brut — la comparaison avec le montant de l'opération reste entièrement
        // de notre ressort, jamais de son côté. Le compte peut être dans une devise différente de l'opération
        // (courant en COMEX) : on convertit le solde vers la devise du dossier avant de comparer.
        var soldeEnDeviseDossier = resultat.SoldeDisponible;
        if (!string.Equals(resultat.Devise, dossier.Devise, StringComparison.OrdinalIgnoreCase))
        {
            var taux = await cbs.ObtenirTauxChangeAsync(resultat.Devise, dossier.Devise, cancellationToken);
            soldeEnDeviseDossier = resultat.SoldeDisponible * taux.Taux;
        }

        resultat.Suffisant = soldeEnDeviseDossier >= (double)dossier.Montant;
        return resultat;
    }
}
