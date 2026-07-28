using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Dossiers.API.Mapping;
using PDOE.Infrastructure;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Dossiers.API.Features.ListDocuments;

public class ListDocumentsHandler(PdoeDbContext db) : IRequestHandler<ListDocumentsQuery, List<DocumentResponse>>
{
    public async Task<List<DocumentResponse>> Handle(ListDocumentsQuery request, CancellationToken cancellationToken)
    {
        var dossierExiste = await db.Dossiers.AnyAsync(d => d.DossierId == request.DossierId, cancellationToken);
        if (!dossierExiste)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        var query = db.Documents.Where(d => d.DossierId == request.DossierId);

        if (request.TypeDocument is { } typeDocument)
        {
            var typeValue = typeDocument.ToString();
            query = query.Where(d => d.TypeDocument == typeValue);
        }

        var documents = await query.OrderBy(d => d.CreatedAt).ToListAsync(cancellationToken);
        return documents.Select(d => d.ToResponse()).ToList();
    }
}
