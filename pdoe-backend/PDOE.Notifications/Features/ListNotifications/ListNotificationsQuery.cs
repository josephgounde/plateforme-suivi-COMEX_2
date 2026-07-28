using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Notifications.Features.ListNotifications;

public record ListNotificationsQuery(StatutNotification? Statut, int? DossierId, CanalNotification? Canal) : IRequest<List<NotificationResponse>>;
