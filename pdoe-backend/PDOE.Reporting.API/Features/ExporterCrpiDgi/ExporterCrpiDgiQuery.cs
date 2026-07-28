using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Reporting.API.Features.ExporterCrpiDgi;

public record ExporterCrpiDgiQuery(ExportReglementaireRequest Request) : IRequest<byte[]>;
