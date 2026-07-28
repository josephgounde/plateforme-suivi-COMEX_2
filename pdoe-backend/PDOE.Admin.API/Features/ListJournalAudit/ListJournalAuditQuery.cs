using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Admin.API.Features.ListJournalAudit;

public record ListJournalAuditQuery : IRequest<List<JournalAuditEntry>>;
