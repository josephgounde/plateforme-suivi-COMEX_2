using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace PDOE.Infrastructure.Archive;

/// Vérifie l'en-tête X-Api-Key contre ArchiveApp:ApiKey — clé partagée utilisée à la fois pour le callback de
/// confirmation (POST .../confirmer-archivage-externe, clé obligatoire, jamais de JWT humain là) et pour le côté
/// pull de l'application d'archivage externe (GET /dossiers, /documents — en alternative à un JWT humain sur ces
/// endpoints précis, cf. DossiersController/DocumentsController). Ne dit rien sur qui peut faire quoi une fois la
/// clé validée : chaque appelant décide s'il exige la clé seule ou l'accepte en plus d'un JWT humain.
public static class ArchiveApiKeyValidator
{
    public static bool CleEstValide(HttpRequest request, IConfiguration configuration)
    {
        var apiKeyAttendue = configuration["ArchiveApp:ApiKey"];
        var apiKeyRecue = request.Headers["X-Api-Key"].ToString();
        return !string.IsNullOrEmpty(apiKeyAttendue) && apiKeyRecue == apiKeyAttendue;
    }
}
