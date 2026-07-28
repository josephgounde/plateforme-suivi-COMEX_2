using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Dossiers.API.Features.UpdateDocumentStatut;

// DTO local pour éviter le nommage instable "BodyN" généré par NSwag (même astuce que RejeterDefinitifRequest).
public record UpdateDocumentStatutRequest(bool EstValide);

public record UpdateDocumentStatutCommand(int DossierId, int DocumentId, UpdateDocumentStatutRequest Request)
    : IRequest<DocumentResponse>;
