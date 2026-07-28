using MediatR;

namespace PDOE.Reporting.API.Features.ExporterRapportActiviteMensuel;

public record ExporterRapportActiviteMensuelQuery(string? Mois) : IRequest<byte[]>;
