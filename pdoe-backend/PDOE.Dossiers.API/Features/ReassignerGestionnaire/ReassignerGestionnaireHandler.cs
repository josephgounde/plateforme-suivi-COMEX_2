using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Dossiers.API.Common;
using PDOE.Dossiers.API.Mapping;
using PDOE.Infrastructure;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Dossiers.API.Features.ReassignerGestionnaire;

public class ReassignerGestionnaireHandler(PdoeDbContext db) : IRequestHandler<ReassignerGestionnaireCommand, DossierResponse>
{
    public async Task<DossierResponse> Handle(ReassignerGestionnaireCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        if (string.IsNullOrWhiteSpace(request.GestionnaireLogin))
        {
            throw new DomainException(400, ErrorResponseCode.GESTIONNAIRE_LOGIN_MANQUANT, "gestionnaireLogin requis.");
        }

        var dossier = await db.Dossiers
            .Include(d => d.EtapesWorkflow)
            .FirstOrDefaultAsync(d => d.DossierId == command.DossierId, cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        // Position dans le circuit ACTIF configuré (WorkflowEtapes.Ordre)
        var actives = await WorkflowEngine.ChargerEtapesActives(db, cancellationToken);
        var indexGestionnaire = actives.FindIndex(e => e.Code == "ETAPE_2_GESTIONNAIRE");
        var indexCourant = actives.FindIndex(e => e.Code == WorkflowEngine.CodeEtapeCourante(dossier));

        if (indexGestionnaire < 0 || indexCourant > indexGestionnaire)
        {
            throw new DomainException(409, ErrorResponseCode.ETAPE_GESTIONNAIRE_DEPASSEE,
                "Ce dossier a déjà dépassé l'étape Gestionnaire — réattribution impossible.");
        }

        var nouveauGestionnaireValide = await db.Utilisateurs.AnyAsync(
            u => u.LoginAD == request.GestionnaireLogin && u.EstActif && u.Profil == "GESTIONNAIRE",
            cancellationToken);

        if (!nouveauGestionnaireValide)
        {
            throw new DomainException(422, ErrorResponseCode.UTILISATEUR_INTROUVABLE,
                "gestionnaireLogin ne correspond à aucun compte actif de profil GESTIONNAIRE.");
        }

        dossier.GestionnaireAssigneLogin = request.GestionnaireLogin;
        dossier.DateConfirmationClient = null;
        dossier.UpdatedAt = DateTime.UtcNow;
        dossier.UpdatedBy = CurrentUser.Login;

        await db.SaveChangesAsync(cancellationToken);

        return dossier.ToResponse();
    }
}
