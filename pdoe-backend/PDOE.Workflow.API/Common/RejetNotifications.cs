using Microsoft.EntityFrameworkCore;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Infrastructure.Notifications;

namespace PDOE.Workflow.API.Common;

/// <summary>Tout rejet (étape, générique ou définitif) notifie les mêmes 4 parties : le gestionnaire
/// du dossier, l'Agent d'accueil qui l'a créé, la Direction et l'Admin DSIRI — peu importe l'étape
/// d'où vient le rejet, contrairement à ValiderEtape/SoumettreDossier qui ne notifient que le titulaire
/// de l'étape suivante.</summary>
internal static class RejetNotifications
{
    public static async Task NotifierPartiesConcernees(
        PdoeDbContext db,
        INotificationSender sender,
        Dossier dossier,
        string typeEvenement,
        CancellationToken cancellationToken)
    {
        var loginsCibles = new HashSet<string> { dossier.CreatedBy };
        if (dossier.GestionnaireAssigneLogin is not null)
            loginsCibles.Add(dossier.GestionnaireAssigneLogin);

        var emailsIndividuels = await db.Utilisateurs
            .Where(u => loginsCibles.Contains(u.LoginAD))
            .Select(u => u.Email)
            .ToListAsync(cancellationToken);

        // Rôles alertés systématiquement sur tout rejet — pas de login fixe : lu depuis Utilisateurs
        // pour rester correct même si Admin DSIRI/Direction changent d'email ou se multiplient.
        var emailsParRole = await db.Utilisateurs
            .Where(u => u.EstActif && (u.Profil == "DIRECTION" || u.Profil == "ADMIN_DSIRI"))
            .Select(u => u.Email)
            .ToListAsync(cancellationToken);

        var destinataires = emailsIndividuels.Concat(emailsParRole).Distinct();

        foreach (var destinataire in destinataires)
        {
            await NotificationWriter.EnregistrerEtEnvoyer(
                db, sender, dossier.DossierId, typeEvenement, destinataire, cancellationToken);
        }
    }
}
