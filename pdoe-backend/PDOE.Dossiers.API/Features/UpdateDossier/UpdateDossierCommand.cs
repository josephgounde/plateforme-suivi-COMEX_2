using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Dossiers.API.Features.UpdateDossier;

public record UpdateDossierCommand(int DossierId, UpdateDossierRequest Request) : IRequest<DossierResponse>;
