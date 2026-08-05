using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Admin.API.Mapping;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Admin.API.Features.CreerUtilisateur;

public class CreerUtilisateurHandler(PdoeDbContext db) : IRequestHandler<CreerUtilisateurCommand, UtilisateurResponse>
{
    public async Task<UtilisateurResponse> Handle(CreerUtilisateurCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        if (string.IsNullOrWhiteSpace(request.LoginAD) || string.IsNullOrWhiteSpace(request.Nom)
            || string.IsNullOrWhiteSpace(request.Prenom) || string.IsNullOrWhiteSpace(request.Email))
        {
            throw new DomainException(400, ErrorResponseCode.CHAMPS_UTILISATEUR_MANQUANTS, "Tous les champs sont requis.");
        }

        var existe = await db.Utilisateurs.AnyAsync(u => u.LoginAD == request.LoginAD, cancellationToken);
        if (existe)
            throw new DomainException(409, ErrorResponseCode.LOGIN_AD_DEJA_UTILISE, "Ce login AD est déjà utilisé.");

        var now = DateTime.UtcNow;
        var utilisateur = new Utilisateur
        {
            LoginAD = request.LoginAD,
            Nom = request.Nom,
            Prenom = request.Prenom,
            Email = request.Email,
            Profil = request.Profil.ToString(),
            EstActif = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = CurrentUser.Login,
            UpdatedBy = CurrentUser.Login,
        };
        db.Utilisateurs.Add(utilisateur);

        db.JournalAudit.Add(new JournalAudit
        {
            Categorie = "UTILISATEUR",
            TypeAction = "UTILISATEUR_CREE",
            Description = $"Compte créé : {utilisateur.LoginAD} ({utilisateur.Profil}).",
            EntiteType = "Utilisateur",
            EntiteId = utilisateur.LoginAD,
            DateAction = now,
            CreatedBy = CurrentUser.Login,
        });

        await db.SaveChangesAsync(cancellationToken);

        return utilisateur.ToResponse();
    }
}
