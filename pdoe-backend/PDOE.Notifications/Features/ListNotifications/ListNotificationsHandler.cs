using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Notifications.Mapping;

namespace PDOE.Notifications.Features.ListNotifications;

public class ListNotificationsHandler(PdoeDbContext db) : IRequestHandler<ListNotificationsQuery, List<NotificationResponse>>
{
    public async Task<List<NotificationResponse>> Handle(ListNotificationsQuery request, CancellationToken cancellationToken)
    {
        var query = db.Notifications.AsQueryable();

        if (request.Statut is not null)
        {
            var statut = request.Statut.ToString()!;
            query = query.Where(n => n.Statut == statut);
        }

        if (request.DossierId is not null)
            query = query.Where(n => n.DossierId == request.DossierId);

        if (request.Canal is not null)
        {
            var canal = request.Canal.ToString()!;
            query = query.Where(n => n.Canal == canal);
        }

        var notifications = await query.OrderByDescending(n => n.CreatedAt).ToListAsync(cancellationToken);
        return notifications.Select(n => n.ToResponse()).ToList();
    }
}
