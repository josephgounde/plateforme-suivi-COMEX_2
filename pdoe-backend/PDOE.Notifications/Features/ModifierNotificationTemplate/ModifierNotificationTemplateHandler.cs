using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Notifications.Mapping;
using PDOE.Shared.Kernel.Common;
using NotificationTemplateResponse = PDOE.Api.Contracts.NotificationTemplate;

namespace PDOE.Notifications.Features.ModifierNotificationTemplate;

public class ModifierNotificationTemplateHandler(PdoeDbContext db) : IRequestHandler<ModifierNotificationTemplateCommand, NotificationTemplateResponse>
{
    public async Task<NotificationTemplateResponse> Handle(ModifierNotificationTemplateCommand command, CancellationToken cancellationToken)
    {
        var template = await db.NotificationTemplates.FirstOrDefaultAsync(t => t.TypeEvenement == command.TypeEvenement, cancellationToken);
        if (template is null)
            throw new DomainException(404, ErrorResponseCode.MODELE_NOTIFICATION_INTROUVABLE, "Modèle de notification introuvable.");

        var request = command.Request;
        if (request.Libelle is not null) template.Libelle = request.Libelle;
        if (request.Message is not null) template.Corps = request.Message;
        if (request.CanalDefaut is not null) template.CanalDefaut = request.CanalDefaut.Value.ToString();
        template.UpdatedAt = DateTime.UtcNow;
        template.UpdatedBy = CurrentUser.Login;

        // Miroir de mockModifierTemplateNotification côté frontend : journalise chaque changement de modèle.
        db.JournalAudit.Add(new JournalAudit
        {
            Categorie = "PARAMETRAGE",
            TypeAction = "MODELE_NOTIFICATION_MODIFIE",
            Description = $"Modèle de notification {template.TypeEvenement} modifié.",
            EntiteType = "NotificationTemplate",
            EntiteId = template.TypeEvenement,
            DateAction = template.UpdatedAt,
            CreatedBy = CurrentUser.Login,
        });

        await db.SaveChangesAsync(cancellationToken);

        return template.ToResponse();
    }
}
