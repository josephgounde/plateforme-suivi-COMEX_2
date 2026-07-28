using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Dossiers.API.Features.NotifierClient;

/// Envoi direct client (SMS/Email), distinct du journal interne AFB. Stateless, comme le mock front.
public class NotifierClientHandler(PdoeDbContext db) : IRequestHandler<NotifierClientCommand, NotifierClientResponse>
{
    public async Task<NotifierClientResponse> Handle(NotifierClientCommand command, CancellationToken cancellationToken)
    {
        var dossier = await db.Dossiers.FirstOrDefaultAsync(d => d.DossierId == command.DossierId, cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        var destinataire = command.Request.Canal == CanalNotification.SMS
            ? dossier.TelephoneClient ?? string.Empty
            : dossier.EmailClient ?? string.Empty;

        return new NotifierClientResponse
        {
            Succes = true,
            Destinataire = destinataire,
        };
    }
}
