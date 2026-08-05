using PDOE.Api.Contracts;
using PDOE.Infrastructure.Entities;

namespace PDOE.Reporting.API.Common;

/// Calcule un DashboardResponse à partir de n'importe quel sous-ensemble de dossiers — dataset global
/// (GetDashboardHandler) ou scopé à un utilisateur (GetMesStatistiquesHandler), même agrégation dans les deux cas.
public static class DashboardAggregator
{
    private static readonly HashSet<string> StatutsUrgents =
    [
        nameof(StatutDossier.ALERTE_J8),
        nameof(StatutDossier.DEPASSE_BCEAO),
        nameof(StatutDossier.ANTI_FRACTIONNEMENT_DETECTE),
    ];

    public static DashboardResponse Agreger(List<Dossier> dossiers)
    {
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
