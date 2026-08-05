using Microsoft.EntityFrameworkCore;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Infrastructure.Notifications;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Dossiers.API.Common;

// Copié dans chaque module (pas de ProjectReference cross-module).
// Silencieux si aucun NotificationTemplate n'existe pour ce typeEvenement.
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
        // Tronqué à la taille de la colonne : un message d'erreur réseau/gateway peut dépasser
        // n'importe quelle limite fixe, et on ne veut jamais qu'une erreur de notification fasse
        // échouer (SqlException de troncature) la transition métier qui l'a déclenchée.
        notification.CodeErreur = resultat.CodeErreur is { Length: > 500 } ? resultat.CodeErreur[..500] : resultat.CodeErreur;

        db.Notifications.Add(notification);
    }
}
