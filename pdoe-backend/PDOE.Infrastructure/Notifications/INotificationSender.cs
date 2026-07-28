namespace PDOE.Infrastructure.Notifications;

/// Abstraction de l'envoi SMS/Email. En dev ça log juste (LocalNotificationSender), en prod ce sera la messagerie interne en HTTP.
public interface INotificationSender
{
    Task<NotificationSendResult> EnvoyerAsync(string canal, string destinataire, string sujet, string corps, CancellationToken cancellationToken);
}

public record NotificationSendResult(bool Succes, string? MessageIdGateway, string? CodeErreur);
