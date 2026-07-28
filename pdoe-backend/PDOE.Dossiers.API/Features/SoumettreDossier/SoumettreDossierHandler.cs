using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Dossiers.API.Common;
using PDOE.Dossiers.API.Mapping;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Infrastructure.Notifications;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Dossiers.API.Features.SoumettreDossier;

public class SoumettreDossierHandler(PdoeDbContext db, INotificationSender sender) : IRequestHandler<SoumettreDossierCommand, DossierResponse>
{
    public async Task<DossierResponse> Handle(SoumettreDossierCommand command, CancellationToken cancellationToken)
    {
        var dossier = await db.Dossiers
            .Include(d => d.EtapesWorkflow)
            .Include(d => d.Documents)
            .FirstOrDefaultAsync(d => d.DossierId == command.DossierId, cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        if (dossier.StatutElectronique != nameof(StatutDossier.BROUILLON))
        {
            throw new DomainException(422, ErrorResponseCode.STATUT_INVALIDE_POUR_ACTION,
                "Le dossier n'est pas en statut BROUILLON — soumission impossible.");
        }

        // FDI obligatoire pour IMPORT_BIENS au-delà de SEUIL_FDI_MONTANT. Vérifié ici (pas à la
        // création) car les documents ne s'attachent qu'après, une fois le DossierId connu.
        if (dossier.TypeOperation == "IMPORT_BIENS")
        {
            var seuilFdi = await db.ParametresMetier
                .Where(p => p.Cle == "SEUIL_FDI_MONTANT")
                .Select(p => (decimal?)decimal.Parse(p.Valeur))
                .FirstOrDefaultAsync(cancellationToken);

            var fdiJointe = dossier.Documents.Any(d => d.TypeDocument == nameof(TypeDocument.FDI));

            if (seuilFdi is not null && dossier.Montant >= seuilFdi && !fdiJointe)
            {
                throw new DomainException(422, ErrorResponseCode.FDI_MANQUANTE,
                    $"FDI (Fiche de Déclaration d'Importation) obligatoire — montant ≥ {seuilFdi} {dossier.Devise} et aucun document FDI joint.");
            }
        }

        var now = DateTime.UtcNow;
        var statutAvant = dossier.StatutElectronique;

        await WorkflowEngine.AvancerVersEtapeSuivante(db, dossier, cancellationToken);
        dossier.UpdatedAt = now;
        dossier.UpdatedBy = CurrentUser.Login;

        var etape = new EtapeWorkflow
        {
            DossierId = dossier.DossierId,
            NiveauValidation = WorkflowEngine.CodeEtapeCourante(dossier),
            StatutAvant = statutAvant,
            StatutApres = dossier.StatutElectronique,
            Action = nameof(ActionWorkflow.SOUMISSION),
            AgentLogin = CurrentUser.Login,
            DateAction = now,
            CreatedAt = now,
            CreatedBy = CurrentUser.Login,
        };
        dossier.EtapesWorkflow.Add(etape);
        JournalAuditWriter.EnregistrerTransition(db, dossier, etape);

        // Notifie le titulaire de la nouvelle étape (Gestionnaire/COMEX/Trésorerie), comme le mock front.
        var destinataireLogin = etape.NiveauValidation switch
        {
            "ETAPE_2_GESTIONNAIRE" => dossier.GestionnaireAssigneLogin,
            "ETAPE_3_COMEX" => "comex",
            "ETAPE_4_TRESORERIE" => "tresorerie",
            _ => null,
        };
        if (destinataireLogin is not null)
        {
            await NotificationWriter.EnregistrerEtEnvoyer(
                db, sender, dossier.DossierId, "DOSSIER_SOUMIS", $"{destinataireLogin}@afbci.ci", cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        return dossier.ToResponse();
    }
}
