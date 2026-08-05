using MediatR;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Gateway.Features.Logout;

/// Rien à révoquer côté serveur (JWT stateless, pas de blacklist) — appelé en best-effort par le frontend
/// (session locale déjà purgée avant cet appel). Ne fait que journaliser.
public class LogoutHandler(PdoeDbContext db) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        db.JournalAudit.Add(new JournalAudit
        {
            Categorie = "AUTHENTIFICATION",
            TypeAction = "DECONNEXION",
            Description = $"Déconnexion — {CurrentUser.Login}.",
            Succes = true,
            DateAction = DateTime.UtcNow,
            CreatedBy = CurrentUser.Login,
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
