using PDOE.Infrastructure.Storage;

namespace PDOE.Dossiers.API.Common;

// Calcule le nom de fichier (type + référence dossier + horodatage) puis délègue l'écriture à
// IFileStorageService.SaveExportAsync — même mécanisme de stockage que les pièces jointes.
internal static class ExportArchiver
{
    public static async Task<FichierArchive> ArchiverAsync(
        IFileStorageService storage,
        string typeExport,
        string referenceDossier,
        byte[] contenu,
        CancellationToken cancellationToken)
    {
        var horodatage = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var nomFichier = $"{typeExport}_{referenceDossier}_{horodatage}.pdf";

        var stocke = await storage.SaveExportAsync(nomFichier, contenu, cancellationToken);

        return new FichierArchive(nomFichier, stocke.CheminStocke, stocke.HashSHA256, stocke.TailleFichier);
    }
}

internal record FichierArchive(string NomFichier, string Chemin, string HashSHA256, long Taille);
