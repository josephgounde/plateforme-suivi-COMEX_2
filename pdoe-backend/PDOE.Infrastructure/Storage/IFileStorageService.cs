namespace PDOE.Infrastructure.Storage;

/// Abstraction du stockage des pièces jointes. En dev ça écrit sur disque local, en prod ce sera le répertoire IIS sécurisé.
public interface IFileStorageService
{
    Task<StoredFile> SaveAsync(int dossierId, string nomFichierOriginal, Stream contenu, CancellationToken cancellationToken);
}

public record StoredFile(string CheminStocke, string HashSHA256, long TailleFichier);
