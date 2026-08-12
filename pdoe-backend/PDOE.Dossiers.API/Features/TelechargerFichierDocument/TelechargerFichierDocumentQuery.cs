using MediatR;

namespace PDOE.Dossiers.API.Features.TelechargerFichierDocument;

public record TelechargerFichierDocumentQuery(int DossierId, int DocumentId) : IRequest<FichierDocument>;

/// Pas un contrat OpenAPI (la réponse est binaire, cf. yaml) — juste de quoi faire un File() côté controller.
public record FichierDocument(byte[] Contenu, string NomFichier);
