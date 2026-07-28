using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Dossiers.API.Features.GetDossier;

public record GetDossierQuery(int DossierId) : IRequest<DossierDetailResponse>;
