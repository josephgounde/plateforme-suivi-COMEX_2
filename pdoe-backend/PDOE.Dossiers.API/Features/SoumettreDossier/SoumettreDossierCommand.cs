using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Dossiers.API.Features.SoumettreDossier;

public record SoumettreDossierCommand(int DossierId) : IRequest<DossierResponse>;
