namespace PDOE.Infrastructure.Storage;

/// Abstraction du stockage des fichiers (pièces jointes ET exports). En dev ça écrit sur disque local, en prod ce sera le répertoire IIS sécurisé.
public interface IFileStorageService
{
    Task<StoredFile> SaveAsync(int dossierId, string nomFichierOriginal, Stream contenu, CancellationToken cancellationToken);

    /// <summary>Exports Reporting/dossier (CRPI, BCEAO, fiche dossier, historique...) — nom de fichier déjà calculé par
    /// l'appelant (porte le type/la période/la référence), contrairement à SaveAsync qui en génère un (GUID).</summary>
    Task<StoredFile> SaveExportAsync(string nomFichier, byte[] contenu, CancellationToken cancellationToken);

    /// <summary>Relit un fichier de pièce jointe à partir du chemin relatif renvoyé par SaveAsync (StoredFile.CheminStocke).
    /// La résolution vers l'emplacement physique reste interne à l'implémentation — ça permet de changer de racine
    /// (migration vers un NAS, etc.) sans invalider les chemins déjà stockés en base. Null si le fichier est introuvable.</summary>
    Task<byte[]?> RecupererAsync(string cheminRelatif, CancellationToken cancellationToken);
}

public record StoredFile(string CheminStocke, string HashSHA256, long TailleFichier);
