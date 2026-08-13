using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Infrastructure.Notifications;
using PDOE.Shared.Kernel.Common;
using PDOE.Workflow.API.Common;

namespace PDOE.Workflow.API.Features.ValiderEtape;

public class ValiderEtapeHandler(PdoeDbContext db, INotificationSender sender) : IRequestHandler<ValiderEtapeCommand, WorkflowTransitionResponse>
{
    public async Task<WorkflowTransitionResponse> Handle(ValiderEtapeCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        var dossier = await db.Dossiers
            .Include(d => d.EtapesWorkflow)
            .FirstOrDefaultAsync(d => d.DossierId == command.DossierId, cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        var codeCourant = WorkflowEngine.CodeEtapeCourante(dossier);

        // Sur une étape générique, niveauValidation reste obligatoire côté contrat mais ne pilote rien — l'avancement suit l'ordre configuré.
        if (dossier.EtapeGeneriqueCode is null && request.NiveauValidation != codeCourant)
        {
            throw new DomainException(422, ErrorResponseCode.STATUT_INVALIDE_POUR_ACTION,
                $"Le dossier est actuellement sur {codeCourant}, pas {request.NiveauValidation}.");
        }

        // Préconditions de l'étape Gestionnaire : commande confirmée ET solde vérifié auprès d'ABS2000 (cf. checklist
        // dossier-detail.component.ts). conformiteBCEAO/lcbftConforme restent sans blocage — aucune UI ne les envoie.
        // Clé sur codeCourant (pas request.NiveauValidation) pour rester valide si repositionné.
        if (codeCourant == "ETAPE_2_GESTIONNAIRE")
        {
            if (dossier.DateConfirmationClient is null)
            {
                throw new DomainException(422, ErrorResponseCode.DATE_CONFIRMATION_CLIENT_MANQUANTE,
                    "La confirmation de commande (dateConfirmationClient) doit être enregistrée avant validation.");
            }

            if (!dossier.SoldeCompteVerifie)
            {
                throw new DomainException(422, ErrorResponseCode.SOLDE_NON_VERIFIE,
                    "Le solde du compte client doit être vérifié auprès d'ABS2000 avant validation.");
            }
        }

        // Précondition de l'étape Trésorerie : disponibilité des fonds confirmée (cf. tresorerie-dashboard.component.ts).
        // Déclarative (aucun contrôle CBS derrière), mais doit bloquer côté serveur comme les préconditions Gestionnaire ci-dessus.
        if (codeCourant == "ETAPE_4_TRESORERIE" && !dossier.DisponibiliteFonds)
        {
            throw new DomainException(422, ErrorResponseCode.DISPONIBILITE_FONDS_NON_CONFIRMEE,
                "La disponibilité des fonds doit être confirmée avant validation.");
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
            Action = nameof(ActionWorkflow.VALIDATION),
            AgentLogin = CurrentUser.Login,
            DateAction = now,
            CreatedAt = now,
            CreatedBy = CurrentUser.Login,
        };
        dossier.EtapesWorkflow.Add(etape);
        JournalAuditWriter.EnregistrerTransition(db, dossier, etape);

        // Notifie le titulaire de l'étape où le dossier vient d'atterrir, quel que soit le chemin pris pour y arriver.
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

        return new WorkflowTransitionResponse
        {
            DossierId = dossier.DossierId,
            ReferenceInterne = dossier.ReferenceInterne,
            StatutAvant = Enum.Parse<StatutDossier>(statutAvant),
            StatutApres = Enum.Parse<StatutDossier>(dossier.StatutElectronique),
            Action = ActionWorkflow.VALIDATION,
            DateAction = now,
        };
    }
}
