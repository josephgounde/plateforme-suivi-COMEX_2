using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Workflow.API.Features.ConfirmerArchivageExterne;

/// Appelé par l'application d'archivage externe (jamais par un utilisateur PDOE — pas de CurrentUser.Login ici,
/// authentification par clé API vérifiée dans le contrôleur) une fois le dossier effectivement récupéré via
/// GET /dossiers?statut=ARCHIVE. Ferme la boucle du scénario hybride.
public class ConfirmerArchivageExterneHandler(PdoeDbContext db) : IRequestHandler<ConfirmerArchivageExterneCommand, ConfirmationArchivageResponse>
{
    public async Task<ConfirmationArchivageResponse> Handle(ConfirmerArchivageExterneCommand command, CancellationToken cancellationToken)
    {
        var dossier = await db.Dossiers.FirstOrDefaultAsync(d => d.DossierId == command.DossierId, cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        if (dossier.StatutElectronique != nameof(StatutDossier.ARCHIVE))
        {
            throw new DomainException(422, ErrorResponseCode.STATUT_INVALIDE_POUR_ACTION,
                "Le dossier n'est pas en statut ARCHIVE — confirmation impossible.");
        }

        var now = DateTime.UtcNow;
        dossier.ArchivageConfirme = true;
        dossier.DateConfirmationArchivage = now;

        await db.SaveChangesAsync(cancellationToken);

        return new ConfirmationArchivageResponse
        {
            DossierId = dossier.DossierId,
            ReferenceInterne = dossier.ReferenceInterne,
            ArchivageConfirme = true,
            DateConfirmationArchivage = now,
        };
    }
}
