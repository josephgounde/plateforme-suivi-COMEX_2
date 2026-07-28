using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Dossiers.API.Features.ListPaiements;

public record ListPaiementsQuery(int DossierId) : IRequest<PaiementListResponse>;
