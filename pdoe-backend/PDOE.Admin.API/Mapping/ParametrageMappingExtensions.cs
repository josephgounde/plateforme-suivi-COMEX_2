using PDOE.Api.Contracts;
using PDOE.Infrastructure.Entities;

namespace PDOE.Admin.API.Mapping;

public static class ParametrageMappingExtensions
{
    public static ParametreMetierResponse ToResponse(this ParametreMetier p) => new()
    {
        ParametreId = p.ParametreId,
        Cle = p.Cle,
        Valeur = p.Valeur,
        Unite = p.Unite,
        Description = p.Description,
        ModifiableUI = p.ModifiableUI,
        ValeurMin = p.ValeurMin,
        ValeurMax = p.ValeurMax,
        UpdatedAt = p.UpdatedAt,
        UpdatedBy = p.UpdatedBy,
    };
}
