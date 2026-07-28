using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Reporting.API.Features.GetDossiersEnRetard;

/// Même simplification que mockDossiersEnRetard() frontend : derniereEtape/heures/seuil sont figés, pas de vrais seuils par étape encore (ParametrageMetier).
public class GetDossiersEnRetardHandler(PdoeDbContext db) : IRequestHandler<GetDossiersEnRetardQuery, List<DossierRetardResponse>>
{
    private static readonly HashSet<string> StatutsUrgents =
    [
        nameof(StatutDossier.ALERTE_J8),
        nameof(StatutDossier.DEPASSE_BCEAO),
        nameof(StatutDossier.ANTI_FRACTIONNEMENT_DETECTE),
    ];

    public async Task<List<DossierRetardResponse>> Handle(GetDossiersEnRetardQuery request, CancellationToken cancellationToken)
    {
        var dossiers = await db.Dossiers
            .Include(d => d.EtapesWorkflow)
            .Where(d => StatutsUrgents.Contains(d.StatutElectronique))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return dossiers.Select(d => new DossierRetardResponse
        {
            DossierId = d.DossierId,
            ReferenceInterne = d.ReferenceInterne,
            NomClient = d.NomClient,
            StatutElectronique = Enum.Parse<StatutDossier>(d.StatutElectronique),
            DerniereEtape = "ETAPE_6_APUREMENT",
            DernierAgent = d.UpdatedBy,
            HeuresDepuisDerniereAction = 48,
            SeuilDepasse = 100,
            EtapesTraverseesCodes = d.EtapesWorkflow
                .OrderBy(e => e.DateAction)
                .Select(e => e.NiveauValidation)
                .ToList(),
        }).ToList();
    }
}
