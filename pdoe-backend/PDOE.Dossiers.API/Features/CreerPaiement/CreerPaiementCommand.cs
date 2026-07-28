using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Dossiers.API.Features.CreerPaiement;

public record CreerPaiementCommand(int DossierId, CreatePaiementRequest Request) : IRequest<PaiementResponse>;
