using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Dossiers.API.Features.CreateDossier;

public record CreateDossierCommand(CreateDossierRequest Request) : IRequest<DossierResponse>;
