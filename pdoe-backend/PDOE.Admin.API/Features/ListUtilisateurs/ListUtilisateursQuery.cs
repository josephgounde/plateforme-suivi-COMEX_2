using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Admin.API.Features.ListUtilisateurs;

public record ListUtilisateursQuery : IRequest<List<UtilisateurResponse>>;
