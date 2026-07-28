using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Reporting.API.Features.GetDossiersEnRetard;

public record GetDossiersEnRetardQuery : IRequest<List<DossierRetardResponse>>;
