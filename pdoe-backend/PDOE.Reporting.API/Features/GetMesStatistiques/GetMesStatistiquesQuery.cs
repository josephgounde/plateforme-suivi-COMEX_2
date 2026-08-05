using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Reporting.API.Features.GetMesStatistiques;

/// Aucun paramètre : le périmètre (dossiers concernés) est déterminé côté handler à partir de CurrentUser.Login,
/// jamais transmis par le client — impossible de consulter les statistiques d'un autre utilisateur.
public record GetMesStatistiquesQuery : IRequest<DashboardResponse>;
