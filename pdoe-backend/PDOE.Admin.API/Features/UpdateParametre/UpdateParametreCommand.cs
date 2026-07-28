using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Admin.API.Features.UpdateParametre;

public record UpdateParametreCommand(string Cle, UpdateParametreRequest Request) : IRequest<ParametreMetierResponse>;
