using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Reporting.API.Features.ExporterSituationBceao;

public record ExporterSituationBceaoQuery(ExportReglementaireRequest Request) : IRequest<byte[]>;
