using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Dossiers.API.Mapping;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Infrastructure.Storage;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Dossiers.API.Features.UploadDocument;

public class UploadDocumentHandler(PdoeDbContext db, IFileStorageService storage)
    : IRequestHandler<UploadDocumentCommand, DocumentResponse>
{
    // Aucun seuil officiel documenté, valeur par défaut raisonnable, à remplacer si
    // une vraie limite métier est un jour spécifiée.
    private const long TailleMaxOctets = 20 * 1024 * 1024;

    public async Task<DocumentResponse> Handle(UploadDocumentCommand command, CancellationToken cancellationToken)
    {
        var statutElectronique = await db.Dossiers
            .Where(d => d.DossierId == command.DossierId)
            .Select(d => d.StatutElectronique)
            .SingleOrDefaultAsync(cancellationToken);

        if (statutElectronique is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        // Dossier archivé (étape 7) — scellé, plus rien ne s'attache, cf. PDOE_DAT §7.2/§7.3.
        if (statutElectronique == nameof(StatutDossier.ARCHIVE))
        {
            throw new DomainException(422, ErrorResponseCode.STATUT_INVALIDE_POUR_ACTION,
                "Dossier archivé — plus aucune modification possible.");
        }

        if (command.Fichier.Length > TailleMaxOctets)
        {
            throw new DomainException(413, ErrorResponseCode.STATUT_INVALIDE_POUR_ACTION,
                $"Fichier trop volumineux (max {TailleMaxOctets / (1024 * 1024)} Mo).");
        }

        await using var contenu = command.Fichier.OpenReadStream();
        var stocke = await storage.SaveAsync(command.DossierId, command.Fichier.FileName, contenu, cancellationToken);

        var now = DateTime.UtcNow;
        var document = new Document
        {
            DossierId = command.DossierId,
            PaiementId = command.PaiementId,
            TypeDocument = command.TypeDocument.ToString(),
            ReferenceDocument = command.ReferenceDocument,
            NomFichier = command.Fichier.FileName,
            CheminIIS = stocke.CheminStocke,
            HashSHA256 = stocke.HashSHA256,
            TailleFichier = stocke.TailleFichier,
            EstObligatoire = command.EstObligatoire,
            EstValide = false,
            CreatedAt = now,
            CreatedBy = CurrentUser.Login,
        };

        db.Documents.Add(document);
        await db.SaveChangesAsync(cancellationToken);

        return document.ToResponse();
    }
}
