namespace PDOE.Infrastructure.Storage;

/// Abstraction du stockage des fichiers (pièces jointes ET exports). En dev ça écrit sur disque local, en prod ce sera le répertoire IIS sécurisé.
public interface IFileStorageService
{
    Task<StoredFile> SaveAsync(int dossierId, string nomFichierOriginal, Stream contenu, CancellationToken cancellationToken);

    /// <summary>Exports Reporting/dossier (CRPI, BCEAO, fiche dossier, historique...) — nom de fichier déjà calculé par
    /// l'appelant (porte le type/la période/la référence), contrairement à SaveAsync qui en génère un (GUID).</summary>
    Task<StoredFile> SaveExportAsync(string nomFichier, byte[] contenu, CancellationToken cancellationToken);
}

public record StoredFile(string CheminStocke, string HashSHA256, long TailleFichier);
