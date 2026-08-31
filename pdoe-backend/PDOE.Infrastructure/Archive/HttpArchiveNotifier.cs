using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace PDOE.Infrastructure.Archive;

/// Client HTTP réel vers l'application d'archivage externe. BaseUrl et clé API (en-tête X-Api-Key, posé au niveau du
/// HttpClient dans Program.cs) configurables via ArchiveApp:BaseUrl / ArchiveApp:ApiKey — à confirmer/ajuster une
/// fois l'URL d'ingestion réelle fournie par l'équipe propriétaire de l'application d'archivage.
public class HttpArchiveNotifier(HttpClient http, ILogger<HttpArchiveNotifier> logger) : IArchiveNotifier
{
    public async Task<bool> NotifierArchivageAsync(int dossierId, string referenceInterne, DateTime dateArchivage, CancellationToken cancellationToken)
    {
        try
        {
            var reponse = await http.PostAsJsonAsync("", new { dossierId, referenceInterne, dateArchivage }, cancellationToken);

            if (!reponse.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Notification d'archivage refusée par l'application externe pour le dossier {DossierId} : {StatusCode}",
                    dossierId, reponse.StatusCode);
                return false;
            }

            return true;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Échec de la notification d'archivage pour le dossier {DossierId}.", dossierId);
            return false;
        }
    }
}
