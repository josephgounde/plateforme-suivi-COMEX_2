using MediatR;
using Microsoft.AspNetCore.Mvc;
using PDOE.Api.Contracts;
using PDOE.Notifications.Features.ListNotifications;

namespace PDOE.Notifications.Controllers;

[ApiController]
[Route("notifications")]
public class NotificationsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<NotificationResponse>>> ListNotifications(
        [FromQuery] StatutNotification? statut,
        [FromQuery] int? dossierId,
        [FromQuery] CanalNotification? canal,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListNotificationsQuery(statut, dossierId, canal), cancellationToken);
        return Ok(result);
    }
}
