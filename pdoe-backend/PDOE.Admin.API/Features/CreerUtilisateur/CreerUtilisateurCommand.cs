using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Admin.API.Features.CreerUtilisateur;

public record CreerUtilisateurCommand(CreerUtilisateurRequest Request) : IRequest<UtilisateurResponse>;
