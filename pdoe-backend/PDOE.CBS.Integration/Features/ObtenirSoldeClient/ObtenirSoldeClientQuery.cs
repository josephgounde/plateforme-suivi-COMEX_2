using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.CBS.Integration.Features.ObtenirSoldeClient;

public record ObtenirSoldeClientQuery(string NumCompte, int DossierId) : IRequest<SoldeClientResult>;
