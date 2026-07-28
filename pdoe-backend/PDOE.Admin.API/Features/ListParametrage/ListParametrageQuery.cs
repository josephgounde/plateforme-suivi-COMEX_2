using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Admin.API.Features.ListParametrage;

public record ListParametrageQuery : IRequest<List<ParametreMetierResponse>>;
