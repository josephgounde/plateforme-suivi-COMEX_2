using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PDOE.Api.Contracts;
using PDOE.Notifications.Features.ListNotificationTemplates;
using PDOE.Notifications.Features.ModifierNotificationTemplate;
using NotificationTemplateResponse = PDOE.Api.Contracts.NotificationTemplate;

namespace PDOE.Notifications.Controllers;

[ApiController]
[Route("notification-templates")]
[Authorize(Policy = "AdminDsiri")]
public class NotificationTemplatesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<NotificationTemplateResponse>>> ListTemplates(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListNotificationTemplatesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{typeEvenement}")]
    public async Task<ActionResult<NotificationTemplateResponse>> ModifierTemplate(
        string typeEvenement,
        [FromBody] NotificationTemplateUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ModifierNotificationTemplateCommand(typeEvenement, request), cancellationToken);
        return Ok(result);
    }
}
