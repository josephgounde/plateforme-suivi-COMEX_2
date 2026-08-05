using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace PDOE.Infrastructure.Storage;

/// Dev/test : écrit sur disque local (Storage:DocumentsRootPath). À remplacer par IIS sécurisé en prod.
/// Charge tout le fichier en mémoire avant de hacher/écrire — ok pour des pièces jointes sous les 20 Mo, pas pour du volumineux.
public class LocalFileStorageService(IConfiguration configuration) : IFileStorageService
{
    public async Task<StoredFile> SaveAsync(int dossierId, string nomFichierOriginal, Stream contenu, CancellationToken cancellationToken)
    {
        var racine = configuration["Storage:DocumentsRootPath"]
            ?? throw new InvalidOperationException("Configuration manquante : Storage:DocumentsRootPath.");

        var dossierCible = Path.Combine(racine, dossierId.ToString());
        Directory.CreateDirectory(dossierCible);

        using var buffer = new MemoryStream();
        await contenu.CopyToAsync(buffer, cancellationToken);
        var octets = buffer.ToArray();

        var hash = Convert.ToHexString(SHA256.HashData(octets)).ToLowerInvariant();
        var nomStocke = $"{Guid.NewGuid()}{Path.GetExtension(nomFichierOriginal)}";
        var cheminComplet = Path.Combine(dossierCible, nomStocke);

        await File.WriteAllBytesAsync(cheminComplet, octets, cancellationToken);

        return new StoredFile(cheminComplet, hash, octets.LongLength);
    }

    public async Task<StoredFile> SaveExportAsync(string nomFichier, byte[] contenu, CancellationToken cancellationToken)
    {
        var racine = configuration["Storage:ReportingArchiveRootPath"]
            ?? throw new InvalidOperationException("Configuration manquante : Storage:ReportingArchiveRootPath.");

        Directory.CreateDirectory(racine);
        var cheminComplet = Path.Combine(racine, nomFichier);

        await File.WriteAllBytesAsync(cheminComplet, contenu, cancellationToken);

        var hash = Convert.ToHexString(SHA256.HashData(contenu)).ToLowerInvariant();
        return new StoredFile(cheminComplet, hash, contenu.LongLength);
    }
}
