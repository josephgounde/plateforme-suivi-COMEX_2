namespace PDOE.Infrastructure.Archive;

/// Abstraction du signal envoyé à l'application d'archivage externe lorsqu'un dossier atteint ARCHIVE (scénario
/// hybride : on pousse un signal léger, l'appli externe vient chercher le détail/documents via notre API,
/// cf. GET /dossiers?statut=ARCHIVE) puis confirme réception via POST /workflow/{dossierId}/confirmer-archivage-externe.
/// Même principe de bascule BaseUrl que ILdapAuthenticator/ICbsClient, à une différence près : un échec ou une
/// absence de configuration ne doit jamais faire échouer l'archivage lui-même (cf. NullArchiveNotifier).
public interface IArchiveNotifier
{
    Task<bool> NotifierArchivageAsync(int dossierId, string referenceInterne, DateTime dateArchivage, CancellationToken cancellationToken);
}
