using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Admin.API.Mapping;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Admin.API.Features.UpdateParametre;

/// Contrairement au mock front (aucune validation), applique vraiment modifiableUI (403) et [valeurMin, valeurMax] (422).
public class UpdateParametreHandler(PdoeDbContext db) : IRequestHandler<UpdateParametreCommand, ParametreMetierResponse>
{
    public async Task<ParametreMetierResponse> Handle(UpdateParametreCommand command, CancellationToken cancellationToken)
    {
        var parametre = await db.ParametresMetier.FirstOrDefaultAsync(p => p.Cle == command.Cle, cancellationToken);

        if (parametre is null)
            throw new DomainException(404, ErrorResponseCode.PARAMETRE_INTROUVABLE, "Paramètre introuvable.");

        if (!parametre.ModifiableUI)
        {
            throw new DomainException(403, ErrorResponseCode.PARAMETRAGE_NON_MODIFIABLE,
                "Ce paramètre n'est pas modifiable via le tableau de bord.");
        }

        var nouvelleValeur = command.Request.Valeur;

        if (parametre.ValeurMin is not null && parametre.ValeurMax is not null)
        {
            if (!int.TryParse(nouvelleValeur, out var valeurInt)
                || !int.TryParse(parametre.ValeurMin, out var min)
                || !int.TryParse(parametre.ValeurMax, out var max)
                || valeurInt < min || valeurInt > max)
            {
                throw new DomainException(422, ErrorResponseCode.VALEUR_HORS_PLAGE,
                    $"La valeur doit être un entier compris entre {parametre.ValeurMin} et {parametre.ValeurMax}.");
            }
        }

        var ancienneValeur = parametre.Valeur;
        var now = DateTime.UtcNow;

        parametre.Valeur = nouvelleValeur;
        parametre.UpdatedAt = now;
        parametre.UpdatedBy = CurrentUser.Login;

        // Miroir de mockUpdateParametre côté front, qui journalise déjà chaque changement.
        db.JournalAudit.Add(new JournalAudit
        {
            Categorie = "PARAMETRAGE",
            TypeAction = "PARAMETRE_MODIFIE",
            Description = $"Paramètre {parametre.Cle} : \"{ancienneValeur}\" → \"{nouvelleValeur}\".",
            EntiteType = "ParametrageMetier",
            EntiteId = parametre.Cle,
            DateAction = now,
            CreatedBy = CurrentUser.Login,
        });

        await db.SaveChangesAsync(cancellationToken);

        return parametre.ToResponse();
    }
}
