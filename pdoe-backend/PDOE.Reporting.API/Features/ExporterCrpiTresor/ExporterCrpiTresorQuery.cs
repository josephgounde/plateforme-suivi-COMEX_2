using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Reporting.API.Features.ExporterCrpiTresor;

public record ExporterCrpiTresorQuery(ExportReglementaireRequest Request) : IRequest<byte[]>;
