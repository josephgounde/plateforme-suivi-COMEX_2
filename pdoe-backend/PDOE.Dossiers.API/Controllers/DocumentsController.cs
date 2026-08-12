using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using PDOE.Api.Contracts;
using PDOE.Dossiers.API.Features.ListDocuments;
using PDOE.Dossiers.API.Features.TelechargerFichierDocument;
using PDOE.Dossiers.API.Features.UpdateDocumentStatut;
using PDOE.Dossiers.API.Features.UploadDocument;

namespace PDOE.Dossiers.API.Controllers;

[ApiController]
[Route("dossiers/{dossierId:int}/documents")]
public class DocumentsController(IMediator mediator) : ControllerBase
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    [HttpGet]
    public async Task<ActionResult<List<DocumentResponse>>> ListDocuments(
        int dossierId,
        [FromQuery] TypeDocument? typeDocument,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListDocumentsQuery(dossierId, typeDocument), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<DocumentResponse>> UploadDocument(
        int dossierId,
        [FromForm] IFormFile fichier,
        [FromForm] TypeDocument typeDocument,
        [FromForm] string? referenceDocument,
        [FromForm] bool estObligatoire,
        [FromForm] int? paiementId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UploadDocumentCommand(dossierId, fichier, typeDocument, referenceDocument, estObligatoire, paiementId),
            cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{documentId:int}")]
    public async Task<ActionResult<DocumentResponse>> UpdateDocumentStatut(
        int dossierId,
        int documentId,
        [FromBody] UpdateDocumentStatutRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateDocumentStatutCommand(dossierId, documentId, request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{documentId:int}/fichier")]
    public async Task<IActionResult> TelechargerFichierDocument(
        int dossierId,
        int documentId,
        CancellationToken cancellationToken)
    {
        var fichier = await mediator.Send(new TelechargerFichierDocumentQuery(dossierId, documentId), cancellationToken);

        if (!ContentTypeProvider.TryGetContentType(fichier.NomFichier, out var contentType))
            contentType = "application/octet-stream";

        return File(fichier.Contenu, contentType, fichier.NomFichier);
    }
}
