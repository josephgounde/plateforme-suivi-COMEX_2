using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Apurement.API.Features.GetAlertes;

public record GetAlertesQuery(int DossierId) : IRequest<List<AlerteApurementResponse>>;
