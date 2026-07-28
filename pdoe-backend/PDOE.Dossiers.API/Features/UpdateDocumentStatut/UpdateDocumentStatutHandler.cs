using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Dossiers.API.Mapping;
using PDOE.Infrastructure;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Dossiers.API.Features.UpdateDocumentStatut;

public class UpdateDocumentStatutHandler(PdoeDbContext db) : IRequestHandler<UpdateDocumentStatutCommand, DocumentResponse>
{
    public async Task<DocumentResponse> Handle(UpdateDocumentStatutCommand command, CancellationToken cancellationToken)
    {
        var document = await db.Documents.FirstOrDefaultAsync(
            d => d.DossierId == command.DossierId && d.DocumentId == command.DocumentId,
            cancellationToken);

        if (document is null)
        {
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE,
                "Document introuvable pour ce dossier.");
        }

        document.EstValide = command.Request.EstValide;

        await db.SaveChangesAsync(cancellationToken);

        return document.ToResponse();
    }
}
