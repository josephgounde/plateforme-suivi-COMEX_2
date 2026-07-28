using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Workflow.API.Features.ControleReglementaire;

public record ControleReglementaireCommand(int DossierId) : IRequest<ControleReglementaireResult>;
