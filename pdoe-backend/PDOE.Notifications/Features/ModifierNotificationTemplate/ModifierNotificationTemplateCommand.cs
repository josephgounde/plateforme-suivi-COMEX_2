using MediatR;
using PDOE.Api.Contracts;
using NotificationTemplateResponse = PDOE.Api.Contracts.NotificationTemplate;

namespace PDOE.Notifications.Features.ModifierNotificationTemplate;

public record ModifierNotificationTemplateCommand(string TypeEvenement, NotificationTemplateUpdateRequest Request) : IRequest<NotificationTemplateResponse>;
