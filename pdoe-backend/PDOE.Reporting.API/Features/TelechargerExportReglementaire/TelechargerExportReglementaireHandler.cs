using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Reporting.API.Features.TelechargerExportReglementaire;

/// Premier code du projet qui relit un fichier déjà archivé (jusqu'ici, Storage n'avait qu'un chemin
/// d'écriture — IFileStorageService.SaveAsync, ExportReglementaireArchiver.ArchiverAsync).
public class TelechargerExportReglementaireHandler(PdoeDbContext db, ILogger<TelechargerExportReglementaireHandler> logger)
    : IRequestHandler<TelechargerExportReglementaireQuery, FichierExporte>
{
    public async Task<FichierExporte> Handle(TelechargerExportReglementaireQuery request, CancellationToken cancellationToken)
    {
        var export = await db.ExportsReglementaires
            .FirstOrDefaultAsync(e => e.ExportReglementaireId == request.ExportReglementaireId, cancellationToken);

        if (export is null)
            throw new DomainException(404, ErrorResponseCode.EXPORT_INTROUVABLE, "Export introuvable.");

        if (!File.Exists(export.CheminFichier))
        {
            throw new DomainException(404, ErrorResponseCode.EXPORT_INTROUVABLE,
                "Le fichier archivé est introuvable sur le disque du serveur.");
        }

        var contenu = await File.ReadAllBytesAsync(export.CheminFichier, cancellationToken);

        // Signale une corruption/altération éventuelle sans bloquer le téléchargement — un utilisateur
        // Direction/COMEX en a besoin même dégradé, mais DSI doit pouvoir enquêter sur l'écart.
        var hash = Convert.ToHexString(SHA256.HashData(contenu)).ToLowerInvariant();
        if (hash != export.HashSHA256)
        {
            logger.LogWarning(
                "Intégrité invalide pour ExportReglementaireId={ExportReglementaireId} : hash attendu {HashAttendu}, hash calculé {HashCalcule}.",
                export.ExportReglementaireId, export.HashSHA256, hash);
        }

        return new FichierExporte(contenu, export.NomFichier);
    }
}
