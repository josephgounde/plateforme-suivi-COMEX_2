using MediatR;
using NotificationTemplateResponse = PDOE.Api.Contracts.NotificationTemplate;

namespace PDOE.Notifications.Features.ListNotificationTemplates;

public record ListNotificationTemplatesQuery : IRequest<List<NotificationTemplateResponse>>;
