using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Admin.API.Mapping;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;

namespace PDOE.Admin.API.Features.ListJournalAudit;

public class ListJournalAuditHandler(PdoeDbContext db) : IRequestHandler<ListJournalAuditQuery, List<JournalAuditEntry>>
{
    public async Task<List<JournalAuditEntry>> Handle(ListJournalAuditQuery request, CancellationToken cancellationToken)
    {
        var entries = await db.JournalAudit.OrderByDescending(j => j.DateAction).ToListAsync(cancellationToken);
        return entries.Select(j => j.ToResponse()).ToList();
    }
}
