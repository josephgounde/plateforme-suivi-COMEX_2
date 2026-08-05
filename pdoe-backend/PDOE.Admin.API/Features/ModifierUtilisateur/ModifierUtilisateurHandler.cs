using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Admin.API.Mapping;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Admin.API.Features.ModifierUtilisateur;

public class ModifierUtilisateurHandler(PdoeDbContext db) : IRequestHandler<ModifierUtilisateurCommand, UtilisateurResponse>
{
    public async Task<UtilisateurResponse> Handle(ModifierUtilisateurCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var utilisateur = await db.Utilisateurs.FirstOrDefaultAsync(u => u.UtilisateurId == command.UtilisateurId, cancellationToken);
        if (utilisateur is null)
            throw new DomainException(404, ErrorResponseCode.UTILISATEUR_INTROUVABLE, "Utilisateur introuvable.");

        // Sans ce garde-fou, un Admin DSIRI encore connecté pourrait se retrouver sans aucun Admin capable de revenir en arrière.
        var estSoiMeme = utilisateur.LoginAD == CurrentUser.Login;
        var etaitAdminDsiri = utilisateur.Profil == nameof(ProfilUtilisateur.ADMIN_DSIRI);
        var retireStatutActif = estSoiMeme && request.EstActif == false;
        var retireProfilAdmin = estSoiMeme && etaitAdminDsiri && request.Profil is not null && request.Profil != ProfilUtilisateur.ADMIN_DSIRI;
        if (retireStatutActif || retireProfilAdmin)
        {
            throw new DomainException(409, ErrorResponseCode.AUTO_RETRAIT_DROITS_ADMIN_INTERDIT,
                "Un Admin DSIRI ne peut pas retirer ses propres droits d'administration.");
        }

        if (request.Nom is not null) utilisateur.Nom = request.Nom;
        if (request.Prenom is not null) utilisateur.Prenom = request.Prenom;
        if (request.Email is not null) utilisateur.Email = request.Email;
        if (request.Profil is not null) utilisateur.Profil = request.Profil.Value.ToString();
        if (request.EstActif is not null) utilisateur.EstActif = request.EstActif.Value;

        utilisateur.UpdatedAt = DateTime.UtcNow;
        utilisateur.UpdatedBy = CurrentUser.Login;

        db.JournalAudit.Add(new JournalAudit
        {
            Categorie = "UTILISATEUR",
            TypeAction = "UTILISATEUR_MODIFIE",
            Description = $"Compte modifié : {utilisateur.LoginAD}.",
            EntiteType = "Utilisateur",
            EntiteId = utilisateur.LoginAD,
            DateAction = utilisateur.UpdatedAt,
            CreatedBy = CurrentUser.Login,
        });

        await db.SaveChangesAsync(cancellationToken);

        return utilisateur.ToResponse();
    }
}
