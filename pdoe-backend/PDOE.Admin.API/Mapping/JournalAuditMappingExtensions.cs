using PDOE.Api.Contracts;
using PDOE.Infrastructure.Entities;

namespace PDOE.Admin.API.Mapping;

public static class JournalAuditMappingExtensions
{
    public static JournalAuditEntry ToResponse(this JournalAudit j) => new()
    {
        JournalAuditId = j.JournalAuditId,
        Categorie = Enum.Parse<CategorieAudit>(j.Categorie),
        TypeAction = j.TypeAction,
        Description = j.Description,
        EntiteType = j.EntiteType,
        EntiteId = j.EntiteId,
        Succes = j.Succes,
        DateAction = j.DateAction,
        CreatedBy = j.CreatedBy,
    };
}
