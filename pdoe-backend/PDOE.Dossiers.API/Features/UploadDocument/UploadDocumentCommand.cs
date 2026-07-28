using MediatR;
using Microsoft.AspNetCore.Http;
using PDOE.Api.Contracts;

namespace PDOE.Dossiers.API.Features.UploadDocument;

public record UploadDocumentCommand(
    int DossierId,
    IFormFile Fichier,
    TypeDocument TypeDocument,
    string? ReferenceDocument,
    bool EstObligatoire,
    int? PaiementId) : IRequest<DocumentResponse>;
