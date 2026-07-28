using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Dossiers.API.Mapping;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Dossiers.API.Features.CreerPaiement;

public class CreerPaiementHandler(PdoeDbContext db) : IRequestHandler<CreerPaiementCommand, PaiementResponse>
{
    public async Task<PaiementResponse> Handle(CreerPaiementCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        var dossier = await db.Dossiers
            .Include(d => d.PaiementsPartiels)
            .FirstOrDefaultAsync(d => d.DossierId == command.DossierId, cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        // Dossier archivé (étape 7 franchie) — scellé, cf. PDOE_DAT §7.2/§7.3.
        if (dossier.StatutElectronique == nameof(StatutDossier.ARCHIVE))
        {
            throw new DomainException(422, ErrorResponseCode.STATUT_INVALIDE_POUR_ACTION,
                "Dossier archivé — plus aucune modification possible.");
        }

        if (dossier.PaiementsPartiels.Any(p => p.ReferencePaiement == request.ReferencePaiement))
        {
            throw new DomainException(422, ErrorResponseCode.REFERENCE_PAIEMENT_DUPLIQUEE,
                "Cette référence de paiement existe déjà pour ce dossier.");
        }

        // Solde courant = SoldeRestantApurement si dispo, sinon le montant initial (comme le mock front).
        var soldeAvant = dossier.SoldeRestantApurement ?? dossier.Montant;
        if (request.MontantPaiement > soldeAvant)
        {
            throw new DomainException(422, ErrorResponseCode.DOUBLE_PAIEMENT_DETECTE,
                "Le montant dépasse le solde restant à apurer.");
        }

        // Max(0, ...) évite un solde "-0" quand le paiement solde pile le restant.
        var soldeRestant = Math.Max(0m, soldeAvant - request.MontantPaiement);
        var now = DateTime.UtcNow;

        var paiement = new PaiementPartiel
        {
            DossierId = dossier.DossierId,
            MontantPaiement = request.MontantPaiement,
            Devise = request.Devise,
            DatePaiement = DateOnly.FromDateTime(request.DatePaiement.UtcDateTime),
            ReferencePaiement = request.ReferencePaiement,
            SoldeRestant = soldeRestant,
            CreatedAt = now,
            CreatedBy = CurrentUser.Login,
        };

        dossier.SoldeRestantApurement = soldeRestant;
        dossier.UpdatedAt = now;
        dossier.UpdatedBy = CurrentUser.Login;

        db.PaiementsPartiels.Add(paiement);

        db.JournalAudit.Add(new JournalAudit
        {
            Categorie = "WORKFLOW",
            TypeAction = "PAIEMENT_PARTIEL_ENREGISTRE",
            Description = $"Dossier {dossier.ReferenceInterne} : paiement partiel de {request.MontantPaiement} {request.Devise}, solde restant {soldeRestant}.",
            EntiteType = "Dossier",
            EntiteId = dossier.DossierId.ToString(),
            DateAction = now,
            CreatedBy = CurrentUser.Login,
        });

        await db.SaveChangesAsync(cancellationToken);

        return paiement.ToResponse();
    }
}
