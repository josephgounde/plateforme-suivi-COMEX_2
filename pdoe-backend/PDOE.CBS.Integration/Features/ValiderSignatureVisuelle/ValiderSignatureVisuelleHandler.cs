using MediatR;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Shared.Kernel.Common;

namespace PDOE.CBS.Integration.Features.ValiderSignatureVisuelle;

/// N'appelle pas ABS2000 : il n'existe aucun service ABS2000 pour "valider visuellement" une signature, seulement
/// pour dire si elle existe (cf. ICbsClient.VerifierSignatureAsync). La comparaison visuelle se fait par l'agent
/// hors PDOE (connexion directe à ABS2000, comparaison avec le document papier) — cette action se contente
/// d'enregistrer localement sa confirmation (qui, quand, avec quelles initiales) pour la trace d'audit.
public class ValiderSignatureVisuelleHandler(PdoeDbContext db) : IRequestHandler<ValiderSignatureVisuelleCommand, bool>
{
    public async Task<bool> Handle(ValiderSignatureVisuelleCommand request, CancellationToken cancellationToken)
    {
        db.JournalAudit.Add(new JournalAudit
        {
            Categorie = "WORKFLOW",
            TypeAction = "SIGNATURE_VISUELLE_CONFIRMEE",
            Description = $"Signature visuelle confirmée pour le compte {request.NumCompte} par l'agent (initiales {request.InitialesAgent}) après comparaison directe dans ABS2000.",
            EntiteType = "Client",
            EntiteId = request.NumCompte,
            DateAction = DateTime.UtcNow,
            CreatedBy = CurrentUser.Login,
        });

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
