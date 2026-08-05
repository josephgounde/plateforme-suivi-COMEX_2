using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Reporting.API.Common;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Reporting.API.Features.GetMesStatistiques;

/// Statistiques personnelles — périmètre différent de GetDashboardHandler (global) selon le profil de
/// l'utilisateur courant : Agent d'accueil (dossiers créés), Gestionnaire (dossiers assignés), Trésorerie
/// (dossiers où l'agent a personnellement agi à l'étape 4 — aucun champ d'assignation sur Dossier pour ce
/// rôle, contrairement aux deux autres). Les autres profils (Direction/COMEX/Admin DSIRI) ont déjà une vue
/// transversale via GetDashboardHandler ; ils reçoivent ici un résultat vide plutôt qu'une erreur.
public class GetMesStatistiquesHandler(PdoeDbContext db) : IRequestHandler<GetMesStatistiquesQuery, DashboardResponse>
{
    public async Task<DashboardResponse> Handle(GetMesStatistiquesQuery request, CancellationToken cancellationToken)
    {
        var login = CurrentUser.Login;

        var utilisateur = await db.Utilisateurs.AsNoTracking()
            .FirstOrDefaultAsync(u => u.LoginAD == login, cancellationToken);

        List<Dossier> dossiers;
        if (utilisateur?.Profil == nameof(ProfilUtilisateur.AGENT_ACCUEIL))
        {
            dossiers = await db.Dossiers.AsNoTracking()
                .Where(d => d.CreatedBy == login)
                .ToListAsync(cancellationToken);
        }
        else if (utilisateur?.Profil == nameof(ProfilUtilisateur.GESTIONNAIRE))
        {
            dossiers = await db.Dossiers.AsNoTracking()
                .Where(d => d.GestionnaireAssigneLogin == login)
                .ToListAsync(cancellationToken);
        }
        else if (utilisateur?.Profil == nameof(ProfilUtilisateur.TRESORERIE))
        {
            dossiers = await DossiersTraitesEnTresorerie(login, cancellationToken);
        }
        else
        {
            dossiers = [];
        }

        return DashboardAggregator.Agreger(dossiers);
    }

    // Pas de champ d'assignation Trésorerie sur Dossier (contrairement à GestionnaireAssigneLogin) — le périmètre
    // se déduit des étapes de workflow que l'agent a personnellement enregistrées à l'étape 4.
    private async Task<List<Dossier>> DossiersTraitesEnTresorerie(string login, CancellationToken cancellationToken)
    {
        var dossierIds = await db.EtapesWorkflow.AsNoTracking()
            .Where(e => e.AgentLogin == login && e.NiveauValidation == "ETAPE_4_TRESORERIE")
            .Select(e => e.DossierId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return await db.Dossiers.AsNoTracking()
            .Where(d => dossierIds.Contains(d.DossierId))
            .ToListAsync(cancellationToken);
    }
}
