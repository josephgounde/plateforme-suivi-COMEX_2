using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace PDOE.Reporting.API.Common;

/// Pas IFileStorageService ici (keyé dossierId, pas adapté à un fichier période). Dossier séparé de DocumentsRootPath, rétention différente.
internal static class ExportReglementaireArchiver
{
    public static async Task<FichierArchive> ArchiverAsync(
        IConfiguration configuration,
        string typeExport,
        DateOnly dateDebut,
        DateOnly dateFin,
        byte[] contenu,
        CancellationToken cancellationToken)
    {
        var racine = configuration["Storage:ReportingArchiveRootPath"]
            ?? throw new InvalidOperationException("Configuration manquante : Storage:ReportingArchiveRootPath.");

        Directory.CreateDirectory(racine);

        var horodatage = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var nomFichier = $"{typeExport}_{dateDebut:yyyyMMdd}_{dateFin:yyyyMMdd}_{horodatage}.xlsx";
        var chemin = Path.Combine(racine, nomFichier);

        await File.WriteAllBytesAsync(chemin, contenu, cancellationToken);

        var hash = Convert.ToHexString(SHA256.HashData(contenu)).ToLowerInvariant();

        return new FichierArchive(nomFichier, chemin, hash, contenu.LongLength);
    }
}

internal record FichierArchive(string NomFichier, string Chemin, string HashSHA256, long Taille);
