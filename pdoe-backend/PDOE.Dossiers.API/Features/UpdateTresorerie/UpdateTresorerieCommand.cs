using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Dossiers.API.Features.UpdateTresorerie;

public record UpdateTresorerieCommand(int DossierId, TresorerieUpdateRequest Request) : IRequest<DossierResponse>;
