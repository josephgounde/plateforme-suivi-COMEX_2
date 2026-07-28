using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Dossiers.API.Features.ReassignerGestionnaire;

public record ReassignerGestionnaireCommand(int DossierId, ReassignerGestionnaireRequest Request) : IRequest<DossierResponse>;
