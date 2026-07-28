using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Apurement.API.Features.DeclarerDepassement;

public record DeclarerDepassementCommand(int DossierId, DepassementRequest Request) : IRequest<DossierResponse>;
