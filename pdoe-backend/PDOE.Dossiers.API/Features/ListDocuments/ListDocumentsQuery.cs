using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Dossiers.API.Features.ListDocuments;

public record ListDocumentsQuery(int DossierId, TypeDocument? TypeDocument) : IRequest<List<DocumentResponse>>;
