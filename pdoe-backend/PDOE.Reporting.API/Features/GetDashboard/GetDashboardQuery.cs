using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Reporting.API.Features.GetDashboard;

public record GetDashboardQuery(string? Periode) : IRequest<DashboardResponse>;
