using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Apurement.API.Common;
using PDOE.Apurement.API.Mapping;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Shared.Kernel.Common;
using ChecklistItemConfigResponse = PDOE.Api.Contracts.ChecklistItemConfig;

namespace PDOE.Apurement.API.Features.ReordonnerChecklistItemsConfig;

public class ReordonnerChecklistItemsConfigHandler(PdoeDbContext db) : IRequestHandler<ReordonnerChecklistItemsConfigCommand, List<ChecklistItemConfigResponse>>
{
    public async Task<List<ChecklistItemConfigResponse>> Handle(ReordonnerChecklistItemsConfigCommand command, CancellationToken cancellationToken)
    {
        var nouvelOrdre = command.Request.Ordre.ToList();

        var items = await db.ChecklistItemsConfig.ToListAsync(cancellationToken);

        var idsExistants = items.Select(i => i.ChecklistItemId).ToHashSet();
        var idsDemandes = nouvelOrdre.ToHashSet();
        if (idsDemandes.Count != nouvelOrdre.Count || !idsExistants.SetEquals(idsDemandes))
        {
            throw new DomainException(422, ErrorResponseCode.CHECKLIST_ORDRE_INVALIDE,
                "La liste fournie ne correspond pas exactement aux items existants.");
        }

        var now = DateTime.UtcNow;
        var itemsParId = items.ToDictionary(i => i.ChecklistItemId);

        // Deux passes pour éviter une collision UNIQUE(Ordre) transitoire — même truc que ReordonnerEtapesConfigHandler.
        var decalageTemporaire = nouvelOrdre.Count;
        foreach (var (id, index) in nouvelOrdre.Select((id, i) => (id, i)))
        {
            itemsParId[id].Ordre = decalageTemporaire + index + 1;
        }
        await db.SaveChangesAsync(cancellationToken);

        foreach (var (id, index) in nouvelOrdre.Select((id, i) => (id, i)))
        {
            var item = itemsParId[id];
            item.Ordre = index + 1;
            item.UpdatedAt = now;
            item.UpdatedBy = CurrentUser.Login;
        }

        // Miroir de mockReordonnerChecklist côté frontend.
        db.JournalAudit.Add(new JournalAudit
        {
            Categorie = "PARAMETRAGE",
            TypeAction = "CHECKLIST_REORDONNEE",
            Description = "Réordonnancement de la checklist d'apurement.",
            EntiteType = "ChecklistItemConfig",
            DateAction = now,
            CreatedBy = CurrentUser.Login,
        });

        await db.SaveChangesAsync(cancellationToken);

        return items.OrderBy(i => i.Ordre).Select(i => i.ToResponse()).ToList();
    }
}
