using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Notifications;

namespace PDOE.Notifications.BackgroundJobs;

/// Referme le "Retry pas encore implémenté" de ModuleMarker.cs : NbTentatives/Statut ECHEC étaient posés en
/// base mais jamais retraités. Relit NOTIFICATION_RETRY_MAX/NOTIFICATION_RETRY_DELAI_MIN (ParametrageMetier)
/// à chaque passage pour rester réactif à un changement depuis l'écran Paramétrage.
public class NotificationRetryService(IServiceScopeFactory scopeFactory, ILogger<NotificationRetryService> logger) : BackgroundService
{
    // Volontairement plus court que le délai minimal par défaut (5 min) pour que celui-ci soit respecté à quelques
    // dizaines de secondes près plutôt qu'en étant lui-même le facteur limitant.
    private static readonly TimeSpan Intervalle = TimeSpan.FromMinutes(1);

    private const string CleRetryMax = "NOTIFICATION_RETRY_MAX";
    private const string CleRetryDelaiMin = "NOTIFICATION_RETRY_DELAI_MIN";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalle);
        do
        {
            try
            {
                await TraiterEchecsEnAttente(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Échec du traitement des retries de notification.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task TraiterEchecsEnAttente(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PdoeDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<INotificationSender>();

        var parametres = await db.ParametresMetier
            .Where(p => p.Cle == CleRetryMax || p.Cle == CleRetryDelaiMin)
            .ToDictionaryAsync(p => p.Cle, p => int.Parse(p.Valeur), cancellationToken);

        // Silencieux si les clés ont été supprimées du référentiel — pas de valeur de retry inventée.
        if (!parametres.TryGetValue(CleRetryMax, out var retryMax) || !parametres.TryGetValue(CleRetryDelaiMin, out var retryDelaiMin))
            return;

        var now = DateTime.UtcNow;
        var seuilDerniereTentative = now.AddMinutes(-retryDelaiMin);

        var echecs = await db.Notifications
            .Where(n => n.Statut == "ECHEC" && n.NbTentatives < retryMax && n.DateEnvoi != null && n.DateEnvoi <= seuilDerniereTentative)
            .ToListAsync(cancellationToken);

        foreach (var notification in echecs)
        {
            var resultat = await sender.EnvoyerAsync(notification.Canal, notification.Destinataire, notification.Sujet ?? "", notification.Corps, cancellationToken);

            notification.NbTentatives++;
            notification.DateEnvoi = now;
            notification.Statut = resultat.Succes ? "ENVOYE" : "ECHEC";
            notification.MessageIdGateway = resultat.MessageIdGateway ?? notification.MessageIdGateway;
            // Tronqué à la taille de la colonne : cf. commentaire équivalent dans NotificationWriter.
            notification.CodeErreur = resultat.Succes ? null
                : resultat.CodeErreur is { Length: > 500 } ? resultat.CodeErreur[..500] : resultat.CodeErreur;
        }

        if (echecs.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("{Count} notification(s) en échec retentée(s).", echecs.Count);
        }
    }
}
