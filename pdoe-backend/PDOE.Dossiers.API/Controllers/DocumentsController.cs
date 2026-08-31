using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using PDOE.Api.Contracts;
using PDOE.Dossiers.API.Features.ListDocuments;
using PDOE.Dossiers.API.Features.TelechargerFichierDocument;
using PDOE.Dossiers.API.Features.UpdateDocumentStatut;
using PDOE.Dossiers.API.Features.UploadDocument;
using PDOE.Infrastructure.Archive;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Dossiers.API.Controllers;

[ApiController]
[Route("dossiers/{dossierId:int}/documents")]
public class DocumentsController(IMediator mediator, IConfiguration configuration) : ControllerBase
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    /// Même raisonnement que DossiersController.ExigerJwtOuCleApi — côté pull du scénario hybride d'archivage externe.
    private void ExigerJwtOuCleApi()
    {
        if (!ArchiveApiKeyValidator.CleEstValide(Request, configuration) && User.Identity?.IsAuthenticated != true)
        {
            throw new DomainException(401, ErrorResponseCode.CLE_API_INVALIDE, "Authentification requise (JWT ou clé API).");
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<DocumentResponse>>> ListDocuments(
        int dossierId,
        [FromQuery] TypeDocument? typeDocument,
        CancellationToken cancellationToken)
    {
        ExigerJwtOuCleApi();

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

    [AllowAnonymous]
    [HttpGet("{documentId:int}/fichier")]
    public async Task<IActionResult> TelechargerFichierDocument(
        int dossierId,
        int documentId,
        CancellationToken cancellationToken)
    {
        ExigerJwtOuCleApi();

        var fichier = await mediator.Send(new TelechargerFichierDocumentQuery(dossierId, documentId), cancellationToken);

        if (!ContentTypeProvider.TryGetContentType(fichier.NomFichier, out var contentType))
            contentType = "application/octet-stream";

        return File(fichier.Contenu, contentType, fichier.NomFichier);
    }
}
