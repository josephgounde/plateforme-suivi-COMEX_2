using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Apurement.API.Common;
using PDOE.Apurement.API.Mapping;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Shared.Kernel.Common;
using ChecklistItemConfigResponse = PDOE.Api.Contracts.ChecklistItemConfig;

namespace PDOE.Apurement.API.Features.ModifierChecklistItemConfig;

public class ModifierChecklistItemConfigHandler(PdoeDbContext db) : IRequestHandler<ModifierChecklistItemConfigCommand, ChecklistItemConfigResponse>
{
    public async Task<ChecklistItemConfigResponse> Handle(ModifierChecklistItemConfigCommand command, CancellationToken cancellationToken)
    {
        var item = await db.ChecklistItemsConfig.FirstOrDefaultAsync(c => c.ChecklistItemId == command.ChecklistItemId, cancellationToken);
        if (item is null)
            throw new DomainException(404, ErrorResponseCode.CHECKLIST_ITEM_INTROUVABLE, "Item de checklist introuvable.");

        var request = command.Request;
        var estActivation = request.Actif == true && !item.Actif;
        var estDesactivation = request.Actif == false && item.Actif;

        if (request.Libelle is not null) item.Libelle = request.Libelle;
        if (request.Actif is not null) item.Actif = request.Actif.Value;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = CurrentUser.Login;

        // Miroir de mockModifierChecklistItem côté frontend.
        var (typeAction, libelleAction) = estActivation ? ("CHECKLIST_ITEM_ACTIVE", "Activation")
            : estDesactivation ? ("CHECKLIST_ITEM_DESACTIVE", "Désactivation")
            : ("CHECKLIST_ITEM_MODIFIE", "Modification");
        db.JournalAudit.Add(new JournalAudit
        {
            Categorie = "PARAMETRAGE",
            TypeAction = typeAction,
            Description = $"{libelleAction} de l'item de checklist « {item.Libelle} ».",
            EntiteType = "ChecklistItemConfig",
            EntiteId = item.ChecklistItemId.ToString(),
            DateAction = item.UpdatedAt,
            CreatedBy = CurrentUser.Login,
        });

        await db.SaveChangesAsync(cancellationToken);

        return item.ToResponse();
    }
}
