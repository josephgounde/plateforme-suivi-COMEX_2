using PDOE.Infrastructure.Entities;
using ChecklistItemConfigResponse = PDOE.Api.Contracts.ChecklistItemConfig;

namespace PDOE.Apurement.API.Mapping;

// Alias requis : contrat et entité EF portent le même nom ChecklistItemConfig (sinon CS0121).
public static class ChecklistMappingExtensions
{
    public static ChecklistItemConfigResponse ToResponse(this ChecklistItemConfig c) => new()
    {
        ChecklistItemId = c.ChecklistItemId,
        Libelle = c.Libelle,
        Ordre = c.Ordre,
        Actif = c.Actif,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
        UpdatedBy = c.UpdatedBy,
    };
}
