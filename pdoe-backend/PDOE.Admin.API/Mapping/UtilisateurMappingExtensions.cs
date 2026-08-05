using PDOE.Api.Contracts;
using PDOE.Infrastructure.Entities;

namespace PDOE.Admin.API.Mapping;

public static class UtilisateurMappingExtensions
{
    public static UtilisateurResponse ToResponse(this Utilisateur u) => new()
    {
        UtilisateurId = u.UtilisateurId,
        LoginAD = u.LoginAD,
        Nom = u.Nom,
        Prenom = u.Prenom,
        Email = u.Email,
        Profil = Enum.Parse<ProfilUtilisateur>(u.Profil),
        EstActif = u.EstActif,
        CreatedAt = u.CreatedAt,
        UpdatedAt = u.UpdatedAt,
        UpdatedBy = u.UpdatedBy,
    };
}
