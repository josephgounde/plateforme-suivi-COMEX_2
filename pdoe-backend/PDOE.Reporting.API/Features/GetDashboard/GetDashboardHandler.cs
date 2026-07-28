using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Reporting.API.Features.GetDashboard;

/// Même simplification que mockDashboard() frontend : "periode" est accepté mais ignoré (pas de date métier sur Dossier), agrège tout.
public class GetDashboardHandler(PdoeDbContext db) : IRequestHandler<GetDashboardQuery, DashboardResponse>
{
    private static readonly HashSet<string> StatutsUrgents =
    [
        nameof(StatutDossier.ALERTE_J8),
        nameof(StatutDossier.DEPASSE_BCEAO),
        nameof(StatutDossier.ANTI_FRACTIONNEMENT_DETECTE),
    ];

    public async Task<DashboardResponse> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var dossiers = await db.Dossiers.AsNoTracking().ToListAsync(cancellationToken);

        var parStatut = dossiers
            .GroupBy(d => d.StatutElectronique)
            .ToDictionary(g => g.Key, g => g.Count());

        var dossiersEnRetard = dossiers.Count(d => StatutsUrgents.Contains(d.StatutElectronique));

        var seuilApurementProche = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
        var dossiersApurementProche = dossiers.Count(d =>
            d.DateEcheanceApurement is { } echeance && echeance < seuilApurementProche);

        var apures = dossiers.Count(d => d.ApurementComplet);
        var enApurementTotal = dossiers.Count(d => d.DateEcheanceApurement is not null);

        return new DashboardResponse
        {
            TotalDossiers = dossiers.Count,
            ParStatut = parStatut,
            DossiersEnRetard = dossiersEnRetard,
            DossiersApurementProche = dossiersApurementProche,
            TauxApurement = enApurementTotal > 0 ? (double)apures / enApurementTotal : 0,
            AlertesNonTraitees = dossiersEnRetard,
        };
    }
}
