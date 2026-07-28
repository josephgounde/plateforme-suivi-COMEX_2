using MediatR;

namespace PDOE.Workflow.API.Features.ExporterHistorique;

public record ExporterHistoriqueQuery(int DossierId) : IRequest<byte[]>;
