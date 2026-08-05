using PDOE.Infrastructure.Entities;
using NotificationTemplateEntity = PDOE.Infrastructure.Entities.NotificationTemplate;
using NotificationTemplateResponse = PDOE.Api.Contracts.NotificationTemplate;

namespace PDOE.Notifications.Mapping;

// Alias nécessaire : contrat et entité EF portent le même nom "NotificationTemplate".
public static class NotificationMappingExtensions
{
    public static PDOE.Api.Contracts.NotificationResponse ToResponse(this Notification n) => new()
    {
        NotificationId = n.NotificationId,
        DossierId = n.DossierId,
        TypeEvenement = n.TypeEvenement,
        Canal = Enum.Parse<PDOE.Api.Contracts.CanalNotification>(n.Canal),
        Destinataire = n.Destinataire,
        Statut = Enum.Parse<PDOE.Api.Contracts.StatutNotification>(n.Statut),
        CodeErreur = n.CodeErreur,
        NbTentatives = n.NbTentatives,
        DateEnvoi = n.DateEnvoi,
        CreatedAt = n.CreatedAt,
    };

    public static NotificationTemplateResponse ToResponse(this NotificationTemplateEntity t) => new()
    {
        TypeEvenement = t.TypeEvenement,
        Libelle = t.Libelle,
        Message = t.Corps,
        CanalDefaut = Enum.Parse<PDOE.Api.Contracts.CanalNotification>(t.CanalDefaut),
        UpdatedAt = t.UpdatedAt,
        UpdatedBy = t.UpdatedBy,
    };
}
