using Microsoft.Extensions.Logging;

namespace PDOE.Infrastructure.Archive;

/// Utilisé tant que ArchiveApp:BaseUrl n'est pas configuré (contrairement à Ldap/Cbs, pas de bascule dev/prod ici :
/// l'archivage ne doit jamais échouer faute d'intégration externe prête). NotifieArchivage reste à false sur le
/// dossier ; à retenter plus tard une fois l'URL réelle renseignée.
public class NullArchiveNotifier(ILogger<NullArchiveNotifier> logger) : IArchiveNotifier
{
    public Task<bool> NotifierArchivageAsync(int dossierId, string referenceInterne, DateTime dateArchivage, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "ArchiveApp:BaseUrl non configuré — signal d'archivage non envoyé pour le dossier {DossierId}.", dossierId);
        return Task.FromResult(false);
    }
}
