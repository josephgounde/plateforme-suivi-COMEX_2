using MediatR;

namespace PDOE.Dossiers.API.Features.GenererFicheDossier;

public record GenererFicheDossierQuery(int DossierId) : IRequest<byte[]>;
