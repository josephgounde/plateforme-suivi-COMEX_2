using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Workflow.API.Features.GetHistorique;

public record GetHistoriqueQuery(int DossierId) : IRequest<List<EtapeWorkflowResponse>>;
