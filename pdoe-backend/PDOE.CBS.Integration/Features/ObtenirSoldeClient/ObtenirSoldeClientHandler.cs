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
        var dossier = await db.Dossiers.FirstOrDefaultAsync(d => d.DossierId == request.DossierId, cancellationToken);

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

        // Seul point d'écriture pour ces champs — cf. UpdateDossierHandler, qui refuse désormais SoldeCompteVerifie
        // pour empêcher qu'un simple PUT ne simule une vérification jamais faite auprès d'ABS2000.
        dossier.SoldeCompteVerifie = true;
        dossier.SoldeSuffisant = resultat.Suffisant;
        dossier.SoldeConstate = (decimal)resultat.SoldeDisponible;
        dossier.DeviseConstatee = resultat.Devise;
        dossier.DateVerificationSolde = DateTime.UtcNow;
        dossier.UpdatedAt = DateTime.UtcNow;
        dossier.UpdatedBy = CurrentUser.Login;
        await db.SaveChangesAsync(cancellationToken);

        return resultat;
    }
}
