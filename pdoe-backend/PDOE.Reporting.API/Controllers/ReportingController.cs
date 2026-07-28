using MediatR;
using Microsoft.AspNetCore.Mvc;
using PDOE.Api.Contracts;
using PDOE.Reporting.API.Features.ExporterCrpiDgi;
using PDOE.Reporting.API.Features.ExporterCrpiTresor;
using PDOE.Reporting.API.Features.ExporterDossiersEnRetard;
using PDOE.Reporting.API.Features.ExporterRapportActiviteMensuel;
using PDOE.Reporting.API.Features.ExporterSituationBceao;
using PDOE.Reporting.API.Features.GetDashboard;
using PDOE.Reporting.API.Features.GetDossiersEnRetard;
using PDOE.Reporting.API.Features.ListExportsReglementaires;
using PDOE.Reporting.API.Features.TelechargerExportReglementaire;

namespace PDOE.Reporting.API.Controllers;

[ApiController]
[Route("reporting")]
public class ReportingController(IMediator mediator) : ControllerBase
{
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardResponse>> GetDashboard(
        [FromQuery] string? periode,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetDashboardQuery(periode), cancellationToken);
        return Ok(result);
    }

    [HttpGet("dossiers-en-retard")]
    public async Task<ActionResult<List<DossierRetardResponse>>> GetDossiersEnRetard(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetDossiersEnRetardQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("dossiers-en-retard/export")]
    public async Task<IActionResult> ExporterDossiersEnRetard(CancellationToken cancellationToken)
    {
        var xlsx = await mediator.Send(new ExporterDossiersEnRetardQuery(), cancellationToken);
        return File(xlsx, XlsxContentType, "dossiers-en-retard.xlsx");
    }

    [HttpGet("activite-mensuelle/export")]
    public async Task<IActionResult> ExporterRapportActiviteMensuel(
        [FromQuery] string? mois,
        CancellationToken cancellationToken)
    {
        var xlsx = await mediator.Send(new ExporterRapportActiviteMensuelQuery(mois), cancellationToken);
        return File(xlsx, XlsxContentType, "activite-mensuelle.xlsx");
    }

    [HttpPost("export-crpi-dgi")]
    public async Task<IActionResult> ExporterCrpiDgi([FromBody] ExportReglementaireRequest request, CancellationToken cancellationToken)
    {
        var xlsx = await mediator.Send(new ExporterCrpiDgiQuery(request), cancellationToken);
        return File(xlsx, XlsxContentType, "crpi-dgi.xlsx");
    }

    [HttpPost("export-crpi-tresor")]
    public async Task<IActionResult> ExporterCrpiTresor([FromBody] ExportReglementaireRequest request, CancellationToken cancellationToken)
    {
        var xlsx = await mediator.Send(new ExporterCrpiTresorQuery(request), cancellationToken);
        return File(xlsx, XlsxContentType, "crpi-tresor.xlsx");
    }

    [HttpPost("export-situation-bceao")]
    public async Task<IActionResult> ExporterSituationBceao([FromBody] ExportReglementaireRequest request, CancellationToken cancellationToken)
    {
        var xlsx = await mediator.Send(new ExporterSituationBceaoQuery(request), cancellationToken);
        return File(xlsx, XlsxContentType, "situation-bceao.xlsx");
    }

    [HttpGet("exports")]
    public async Task<ActionResult<ExportReglementaireListResponse>> ListExportsReglementaires(
        [FromQuery] CategorieExport? categorie,
        [FromQuery] TypeExport? typeExport,
        [FromQuery] DateOnly? dateDebut,
        [FromQuery] DateOnly? dateFin,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new ListExportsReglementairesQuery(categorie, typeExport, dateDebut, dateFin, page, pageSize),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("exports/{exportReglementaireId:int}/download")]
    public async Task<IActionResult> TelechargerExportReglementaire(int exportReglementaireId, CancellationToken cancellationToken)
    {
        var fichier = await mediator.Send(new TelechargerExportReglementaireQuery(exportReglementaireId), cancellationToken);
        return File(fichier.Contenu, XlsxContentType, fichier.NomFichier);
    }
}
