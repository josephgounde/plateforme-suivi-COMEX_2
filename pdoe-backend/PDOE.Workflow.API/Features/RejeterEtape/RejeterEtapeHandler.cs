using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Infrastructure.Notifications;
using PDOE.Shared.Kernel.Common;
using PDOE.Workflow.API.Common;

namespace PDOE.Workflow.API.Features.RejeterEtape;

public class RejeterEtapeHandler(PdoeDbContext db, INotificationSender sender) : IRequestHandler<RejeterEtapeCommand, WorkflowTransitionResponse>
{
    public async Task<WorkflowTransitionResponse> Handle(RejeterEtapeCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        if (string.IsNullOrWhiteSpace(request.MotifRejet))
            throw new DomainException(400, ErrorResponseCode.MOTIF_REJET_MANQUANT, "motifRejet requis.");

        var dossier = await db.Dossiers
            .Include(d => d.EtapesWorkflow)
            .FirstOrDefaultAsync(d => d.DossierId == command.DossierId, cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        var codeCourant = WorkflowEngine.CodeEtapeCourante(dossier);
        var now = DateTime.UtcNow;
        var statutAvant = dossier.StatutElectronique;

        if (dossier.EtapeGeneriqueCode is not null)
        {
            // Étape générique : pas de cible configurable, le rejet marque juste REJETE sans déplacer le dossier.
            dossier.SousEtatGenerique = nameof(SousEtat.REJETE);
            dossier.UpdatedAt = now;
            dossier.UpdatedBy = CurrentUser.Login;

            var etapeGenerique = new EtapeWorkflow
            {
                DossierId = dossier.DossierId,
                NiveauValidation = codeCourant,
                StatutAvant = statutAvant,
                StatutApres = dossier.StatutElectronique,
                Action = nameof(ActionWorkflow.REJET),
                MotifRejet = request.MotifRejet,
                // La contrainte CK exige une valeur dès Action=REJET même sans vraie cible — même marqueur que RejeterDefinitifHandler.
                ResponsableCorrection = "AUCUN",
                AgentLogin = CurrentUser.Login,
                DateAction = now,
                CreatedAt = now,
                CreatedBy = CurrentUser.Login,
            };
            dossier.EtapesWorkflow.Add(etapeGenerique);
            JournalAuditWriter.EnregistrerTransition(db, dossier, etapeGenerique);

            await db.SaveChangesAsync(cancellationToken);

            return new WorkflowTransitionResponse
            {
                DossierId = dossier.DossierId,
                ReferenceInterne = dossier.ReferenceInterne,
                StatutAvant = Enum.Parse<StatutDossier>(statutAvant),
                StatutApres = Enum.Parse<StatutDossier>(dossier.StatutElectronique),
                Action = ActionWorkflow.REJET,
                DateAction = now,
            };
        }

        if (request.NiveauValidation != codeCourant)
        {
            throw new DomainException(422, ErrorResponseCode.STATUT_INVALIDE_POUR_ACTION,
                $"Le dossier est actuellement sur {codeCourant}, pas {request.NiveauValidation}.");
        }

        if (string.IsNullOrWhiteSpace(request.ResponsableCorrection))
            throw new DomainException(400, ErrorResponseCode.RESPONSABLE_CORRECTION_MANQUANT, "responsableCorrection requis.");

        var actives = await WorkflowEngine.ChargerEtapesActives(db, cancellationToken);
        var indexActuel = actives.FindIndex(e => e.Code == codeCourant);
        var cible = actives.FirstOrDefault(e => e.Code == request.ResponsableCorrection);
        var indexCible = cible is null ? -1 : actives.IndexOf(cible);

        if (cible is null || indexCible > indexActuel)
        {
            throw new DomainException(422, ErrorResponseCode.STATUT_INVALIDE_POUR_ACTION,
                "responsableCorrection doit désigner une étape active déjà franchie, jamais en avance.");
        }

        WorkflowEngine.AtterrirSur(dossier, cible);

        // Rejet jusqu'à l'Agent d'accueil = repart de zéro, la confirmation Gestionnaire n'a plus de sens tant qu'il n'a pas retraversé son étape.
        if (cible.Code == "ETAPE_1_INITIATION")
        {
            dossier.DateConfirmationClient = null;
        }

        dossier.UpdatedAt = now;
        dossier.UpdatedBy = CurrentUser.Login;

        var etape = new EtapeWorkflow
        {
            DossierId = dossier.DossierId,
            NiveauValidation = codeCourant,
            StatutAvant = statutAvant,
            StatutApres = dossier.StatutElectronique,
            Action = nameof(ActionWorkflow.REJET),
            MotifRejet = request.MotifRejet,
            ResponsableCorrection = request.ResponsableCorrection,
            AgentLogin = CurrentUser.Login,
            DateAction = now,
            CreatedAt = now,
            CreatedBy = CurrentUser.Login,
        };
        dossier.EtapesWorkflow.Add(etape);
        JournalAuditWriter.EnregistrerTransition(db, dossier, etape);

        // Notifie le profil qui doit corriger le dossier.
        var destinataireLogin = cible.Code switch
        {
            "ETAPE_1_INITIATION" => dossier.CreatedBy,
            "ETAPE_2_GESTIONNAIRE" => dossier.GestionnaireAssigneLogin,
            "ETAPE_3_COMEX" => "comex",
            "ETAPE_4_TRESORERIE" => "tresorerie",
            _ => null,
        };
        if (destinataireLogin is not null)
        {
            await NotificationWriter.EnregistrerEtEnvoyer(
                db, sender, dossier.DossierId, "DOSSIER_REJETE", $"{destinataireLogin}@afbci.ci", cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        return new WorkflowTransitionResponse
        {
            DossierId = dossier.DossierId,
            ReferenceInterne = dossier.ReferenceInterne,
            StatutAvant = Enum.Parse<StatutDossier>(statutAvant),
            StatutApres = Enum.Parse<StatutDossier>(dossier.StatutElectronique),
            Action = ActionWorkflow.REJET,
            DateAction = now,
        };
    }
}
