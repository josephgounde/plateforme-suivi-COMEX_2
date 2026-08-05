using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Reporting.API.Common;

namespace PDOE.Reporting.API.Features.GetDashboard;

/// Même simplification que mockDashboard() frontend : "periode" est accepté mais ignoré (pas de date métier sur Dossier), agrège tout.
public class GetDashboardHandler(PdoeDbContext db) : IRequestHandler<GetDashboardQuery, DashboardResponse>
{
    public async Task<DashboardResponse> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var dossiers = await db.Dossiers.AsNoTracking().ToListAsync(cancellationToken);
        return DashboardAggregator.Agreger(dossiers);
    }
}
