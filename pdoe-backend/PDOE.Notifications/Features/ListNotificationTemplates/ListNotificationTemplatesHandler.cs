using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Infrastructure;
using PDOE.Notifications.Mapping;
using NotificationTemplateResponse = PDOE.Api.Contracts.NotificationTemplate;

namespace PDOE.Notifications.Features.ListNotificationTemplates;

public class ListNotificationTemplatesHandler(PdoeDbContext db) : IRequestHandler<ListNotificationTemplatesQuery, List<NotificationTemplateResponse>>
{
    public async Task<List<NotificationTemplateResponse>> Handle(ListNotificationTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await db.NotificationTemplates.OrderBy(t => t.TypeEvenement).ToListAsync(cancellationToken);
        return templates.Select(t => t.ToResponse()).ToList();
    }
}
