using MediatR;

namespace PDOE.Reporting.API.Features.ExporterDossiersEnRetard;

public record ExporterDossiersEnRetardQuery : IRequest<byte[]>;
