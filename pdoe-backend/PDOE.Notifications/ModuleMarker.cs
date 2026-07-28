namespace PDOE.Notifications;

/// Gateway SMS/Email + modèles + journalisation. Envoi via INotificationSender (LocalNotificationSender en dev).
/// Retry géré par NotificationRetryService (BackgroundJobs), lit NOTIFICATION_RETRY_MAX/DELAI_MIN à chaque passage.
public static class ModuleMarker
{
}
