using MediatR;

namespace PDOE.Reporting.API.Features.TelechargerExportReglementaire;

public record TelechargerExportReglementaireQuery(int ExportReglementaireId) : IRequest<FichierExporte>;

/// Pas un contrat OpenAPI (la réponse est binaire, cf. yaml) — juste de quoi faire un File() côté controller.
public record FichierExporte(byte[] Contenu, string NomFichier);
