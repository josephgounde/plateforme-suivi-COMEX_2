using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Apurement.API.Common;
using PDOE.Apurement.API.Mapping;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Shared.Kernel.Common;
using ChecklistItemConfigEntity = PDOE.Infrastructure.Entities.ChecklistItemConfig;
using ChecklistItemConfigResponse = PDOE.Api.Contracts.ChecklistItemConfig;

namespace PDOE.Apurement.API.Features.CreerChecklistItemConfig;

public class CreerChecklistItemConfigHandler(PdoeDbContext db) : IRequestHandler<CreerChecklistItemConfigCommand, ChecklistItemConfigResponse>
{
    public async Task<ChecklistItemConfigResponse> Handle(CreerChecklistItemConfigCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        if (string.IsNullOrWhiteSpace(request.Libelle))
            throw new DomainException(400, ErrorResponseCode.CHECKLIST_ITEM_LIBELLE_MANQUANT, "libelle est requis.");

        var now = DateTime.UtcNow;
        var ordre = await db.ChecklistItemsConfig.CountAsync(cancellationToken) + 1;

        var item = new ChecklistItemConfigEntity
        {
            Libelle = request.Libelle,
            Ordre = ordre,
            Actif = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = CurrentUser.Login,
            UpdatedBy = CurrentUser.Login,
        };

        db.ChecklistItemsConfig.Add(item);
        // ChecklistItemId (IDENTITY) n'existe qu'après ce SaveChanges, donc rien à mettre dans EntiteId avant.
        await db.SaveChangesAsync(cancellationToken);

        // Miroir de mockCreerChecklistItem côté frontend.
        db.JournalAudit.Add(new JournalAudit
        {
            Categorie = "PARAMETRAGE",
            TypeAction = "CHECKLIST_ITEM_CREE",
            Description = $"Ajout de l'item de checklist « {item.Libelle} ».",
            EntiteType = "ChecklistItemConfig",
            EntiteId = item.ChecklistItemId.ToString(),
            DateAction = now,
            CreatedBy = CurrentUser.Login,
        });

        await db.SaveChangesAsync(cancellationToken);

        return item.ToResponse();
    }
}
