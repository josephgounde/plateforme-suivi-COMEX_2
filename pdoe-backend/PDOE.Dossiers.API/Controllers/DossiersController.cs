using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PDOE.Api.Contracts;
using PDOE.Dossiers.API.Features.CreateDossier;
using PDOE.Dossiers.API.Features.CreerPaiement;
using PDOE.Dossiers.API.Features.GenererFicheDossier;
using PDOE.Dossiers.API.Features.GetDossier;
using PDOE.Dossiers.API.Features.ListDossiers;
using PDOE.Dossiers.API.Features.ListPaiements;
using PDOE.Dossiers.API.Features.NotifierClient;
using PDOE.Dossiers.API.Features.ReassignerGestionnaire;
using PDOE.Dossiers.API.Features.SoumettreDossier;
using PDOE.Dossiers.API.Features.UpdateDossier;
using PDOE.Dossiers.API.Features.UpdateTresorerie;
using PDOE.Infrastructure.Archive;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Dossiers.API.Controllers;

[ApiController]
[Route("dossiers")]
public class DossiersController(IMediator mediator, IConfiguration configuration) : ControllerBase
{
    /// GET /dossiers et GET /dossiers/{id} sont accessibles à l'application d'archivage externe (clé API, cf.
    /// ArchiveApiKeyValidator) en plus des utilisateurs PDOE (JWT) — c'est le côté "pull" du scénario hybride
    /// de handoff vers l'archivage (statut=ARCHIVE). [AllowAnonymous] retire seulement l'obligation de JWT ;
    /// un appel sans clé API ni JWT valide est toujours rejeté explicitement ci-dessous.
    private void ExigerJwtOuCleApi()
    {
        if (!ArchiveApiKeyValidator.CleEstValide(Request, configuration) && User.Identity?.IsAuthenticated != true)
        {
            throw new DomainException(401, ErrorResponseCode.CLE_API_INVALIDE, "Authentification requise (JWT ou clé API).");
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<DossierListResponse>> ListDossiers(
        [FromQuery] StatutDossier? statut,
        [FromQuery] TypeOperation? typeOperation,
        [FromQuery] string? numCompte,
        [FromQuery] DateOnly? dateDebutCreation,
        [FromQuery] DateOnly? dateFinCreation,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        ExigerJwtOuCleApi();

        var result = await mediator.Send(
            new ListDossiersQuery(statut, typeOperation, numCompte, dateDebutCreation, dateFinCreation, page, pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<DossierResponse>> CreateDossier(
        [FromBody] CreateDossierRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateDossierCommand(request), cancellationToken);
        return CreatedAtAction(nameof(GetDossier), new { dossierId = result.DossierId }, result);
    }

    [AllowAnonymous]
    [HttpGet("{dossierId:int}")]
    public async Task<ActionResult<DossierDetailResponse>> GetDossier(int dossierId, CancellationToken cancellationToken)
    {
        ExigerJwtOuCleApi();

        var result = await mediator.Send(new GetDossierQuery(dossierId), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{dossierId:int}")]
    public async Task<ActionResult<DossierResponse>> UpdateDossier(
        int dossierId,
        [FromBody] UpdateDossierRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateDossierCommand(dossierId, request), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{dossierId:int}/soumettre")]
    public async Task<ActionResult<DossierResponse>> SoumettreDossier(
        int dossierId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SoumettreDossierCommand(dossierId), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{dossierId:int}/tresorerie")]
    public async Task<ActionResult<DossierResponse>> UpdateTresorerie(
        int dossierId,
        [FromBody] TresorerieUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateTresorerieCommand(dossierId, request), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{dossierId:int}/gestionnaire")]
    public async Task<ActionResult<DossierResponse>> ReassignerGestionnaire(
        int dossierId,
        [FromBody] ReassignerGestionnaireRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ReassignerGestionnaireCommand(dossierId, request), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{dossierId:int}/notifier-client")]
    public async Task<ActionResult<NotifierClientResponse>> NotifierClient(
        int dossierId,
        [FromBody] NotifierClientRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new NotifierClientCommand(dossierId, request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{dossierId:int}/paiements-partiels")]
    public async Task<ActionResult<PaiementListResponse>> ListPaiements(
        int dossierId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListPaiementsQuery(dossierId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{dossierId:int}/paiements-partiels")]
    public async Task<ActionResult<PaiementResponse>> CreerPaiement(
        int dossierId,
        [FromBody] CreatePaiementRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreerPaiementCommand(dossierId, request), cancellationToken);
        return StatusCode(201, result);
    }

    [HttpGet("{dossierId:int}/fiche")]
    public async Task<IActionResult> GenererFicheDossier(int dossierId, CancellationToken cancellationToken)
    {
        var pdf = await mediator.Send(new GenererFicheDossierQuery(dossierId), cancellationToken);
        return File(pdf, "application/pdf");
    }
}
