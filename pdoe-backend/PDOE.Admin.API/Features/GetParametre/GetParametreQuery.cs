using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Admin.API.Features.GetParametre;

public record GetParametreQuery(string Cle) : IRequest<ParametreMetierResponse>;
