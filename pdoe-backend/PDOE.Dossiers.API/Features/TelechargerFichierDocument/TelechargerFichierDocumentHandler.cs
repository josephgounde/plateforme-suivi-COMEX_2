using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Storage;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Dossiers.API.Features.TelechargerFichierDocument;

public class TelechargerFichierDocumentHandler(PdoeDbContext db, IFileStorageService storage, ILogger<TelechargerFichierDocumentHandler> logger)
    : IRequestHandler<TelechargerFichierDocumentQuery, FichierDocument>
{
    public async Task<FichierDocument> Handle(TelechargerFichierDocumentQuery request, CancellationToken cancellationToken)
    {
        var document = await db.Documents
            .FirstOrDefaultAsync(d => d.DocumentId == request.DocumentId && d.DossierId == request.DossierId, cancellationToken);

        if (document is null)
            throw new DomainException(404, ErrorResponseCode.DOCUMENT_INTROUVABLE, "Document introuvable.");

        var contenu = await storage.RecupererAsync(document.CheminIIS, cancellationToken);
        if (contenu is null)
        {
            throw new DomainException(404, ErrorResponseCode.DOCUMENT_INTROUVABLE,
                "Le fichier est introuvable sur le disque du serveur.");
        }

        // Signale une corruption/altération éventuelle sans bloquer l'aperçu — cf. TelechargerExportReglementaireHandler.
        var hash = Convert.ToHexString(SHA256.HashData(contenu)).ToLowerInvariant();
        if (hash != document.HashSHA256)
        {
            logger.LogWarning(
                "Intégrité invalide pour DocumentId={DocumentId} : hash attendu {HashAttendu}, hash calculé {HashCalcule}.",
                document.DocumentId, document.HashSHA256, hash);
        }

        return new FichierDocument(contenu, document.NomFichier);
    }
}
