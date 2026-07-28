using MediatR;
using Microsoft.AspNetCore.Mvc;
using PDOE.Api.Contracts;
using PDOE.Workflow.API.Features.ArchiverDossier;
using PDOE.Workflow.API.Features.ControleLcbft;
using PDOE.Workflow.API.Features.ControleReglementaire;
using PDOE.Workflow.API.Features.ExporterHistorique;
using PDOE.Workflow.API.Features.GetHistorique;
using PDOE.Workflow.API.Features.LeverAlerte;
using PDOE.Workflow.API.Features.RejeterDefinitif;
using PDOE.Workflow.API.Features.RejeterEtape;
using PDOE.Workflow.API.Features.SignalerFractionnement;
using PDOE.Workflow.API.Features.ValiderEtape;

namespace PDOE.Workflow.API.Controllers;

[ApiController]
[Route("workflow")]
public class WorkflowController(IMediator mediator) : ControllerBase
{
    [HttpPost("{dossierId:int}/valider")]
    public async Task<ActionResult<WorkflowTransitionResponse>> Valider(
        int dossierId,
        [FromBody] ValiderEtapeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ValiderEtapeCommand(dossierId, request), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{dossierId:int}/rejeter")]
    public async Task<ActionResult<WorkflowTransitionResponse>> Rejeter(
        int dossierId,
        [FromBody] RejeterEtapeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RejeterEtapeCommand(dossierId, request), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{dossierId:int}/lever-alerte")]
    public async Task<ActionResult<WorkflowTransitionResponse>> LeverAlerte(int dossierId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new LeverAlerteCommand(dossierId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{dossierId:int}/rejeter-definitif")]
    public async Task<ActionResult<WorkflowTransitionResponse>> RejeterDefinitif(
        int dossierId,
        [FromBody] RejeterDefinitifRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RejeterDefinitifCommand(dossierId, request), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{dossierId:int}/signaler-fractionnement")]
    public async Task<ActionResult<WorkflowTransitionResponse>> SignalerFractionnement(
        int dossierId,
        [FromBody] SignalerFractionnementRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SignalerFractionnementCommand(dossierId, request), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{dossierId:int}/archiver")]
    public async Task<ActionResult<WorkflowTransitionResponse>> Archiver(int dossierId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ArchiverDossierCommand(dossierId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{dossierId:int}/controle-reglementaire")]
    public async Task<ActionResult<ControleReglementaireResult>> ControleReglementaire(
        int dossierId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ControleReglementaireCommand(dossierId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{dossierId:int}/controle-lcbft")]
    public async Task<ActionResult<ControleLcbftResult>> ControleLcbft(
        int dossierId,
        [FromBody] ControleLcbftRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ControleLcbftCommand(dossierId, request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{dossierId:int}/historique")]
    public async Task<ActionResult<List<EtapeWorkflowResponse>>> GetHistorique(int dossierId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetHistoriqueQuery(dossierId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{dossierId:int}/historique/export")]
    public async Task<IActionResult> ExporterHistorique(int dossierId, CancellationToken cancellationToken)
    {
        var pdf = await mediator.Send(new ExporterHistoriqueQuery(dossierId), cancellationToken);
        return File(pdf, "application/pdf");
    }
}
