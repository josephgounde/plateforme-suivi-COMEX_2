using Microsoft.Extensions.Logging;

namespace PDOE.Infrastructure.Notifications;

/// Version dev/test, journalise sans réseau. À remplacer par un vrai client HTTP (ex. HttpNotificationSender) via DI dans Program.cs.
public class LocalNotificationSender(ILogger<LocalNotificationSender> logger) : INotificationSender
{
    public Task<NotificationSendResult> EnvoyerAsync(string canal, string destinataire, string sujet, string corps, CancellationToken cancellationToken)
    {
        logger.LogInformation("[Notification-{Canal}] -> {Destinataire} : {Sujet}", canal, destinataire, sujet);
        return Task.FromResult(new NotificationSendResult(true, $"LOCAL-{Guid.NewGuid():N}", null));
    }
}
