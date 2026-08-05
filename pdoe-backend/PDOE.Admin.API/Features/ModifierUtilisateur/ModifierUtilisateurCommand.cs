using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Admin.API.Features.ModifierUtilisateur;

public record ModifierUtilisateurCommand(int UtilisateurId, ModifierUtilisateurRequest Request) : IRequest<UtilisateurResponse>;
