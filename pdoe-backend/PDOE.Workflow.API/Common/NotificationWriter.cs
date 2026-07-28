using Microsoft.EntityFrameworkCore;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Infrastructure.Notifications;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Workflow.API.Common;

// Dupliqué dans chaque module, pas de ProjectReference cross-module. Silencieux si aucun NotificationTemplate pour le typeEvenement.
internal static class NotificationWriter
{
    public static async Task EnregistrerEtEnvoyer(
        PdoeDbContext db,
        INotificationSender sender,
        int? dossierId,
        string typeEvenement,
        string destinataire,
        CancellationToken cancellationToken)
    {
        var template = await db.NotificationTemplates.FirstOrDefaultAsync(t => t.TypeEvenement == typeEvenement, cancellationToken);
        if (template is null) return;

        var now = DateTime.UtcNow;
        var notification = new Notification
        {
            DossierId = dossierId,
            TypeEvenement = typeEvenement,
            Canal = template.CanalDefaut,
            Destinataire = destinataire,
            Sujet = template.Libelle,
            Corps = template.Corps,
            NbTentatives = 1,
            DateEnvoi = now,
            CreatedAt = now,
            CreatedBy = CurrentUser.Login,
        };

        var resultat = await sender.EnvoyerAsync(notification.Canal, destinataire, notification.Sujet, notification.Corps, cancellationToken);
        notification.Statut = resultat.Succes ? "ENVOYE" : "ECHEC";
        notification.MessageIdGateway = resultat.MessageIdGateway;
        notification.CodeErreur = resultat.CodeErreur;

        db.Notifications.Add(notification);
    }
}
