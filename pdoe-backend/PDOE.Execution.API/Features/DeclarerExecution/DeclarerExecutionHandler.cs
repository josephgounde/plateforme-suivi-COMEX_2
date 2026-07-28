using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Execution.API.Common;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Execution.API.Features.DeclarerExecution;

/// Enregistre l'exécution SWIFT, calcule l'échéance d'apurement et planifie les alertes J-14/J-8/J0.
/// L'envoi effectif est déclenché par AlerteApurementSchedulerService (PDOE.Workflow.API/BackgroundJobs)
/// quand DateAlerte est atteinte, pas ici.
public class DeclarerExecutionHandler(PdoeDbContext db) : IRequestHandler<DeclarerExecutionCommand, ExecutionDeclarationResponse>
{
    // EXPORT_BIENS est le seul type à deux clés : paiement (120j, Annexe II Art.13/16) + rapatriement
    // (30j, Art.15/17) qui s'additionnent. Lu depuis ParametrageMetier pour rester ajustable.
    private static readonly IReadOnlyDictionary<string, string[]> ClesDelaiParType = new Dictionary<string, string[]>
    {
        ["IMPORT_BIENS"] = ["DELAI_APUREMENT_IMPORT_BIENS_J"],
        ["IMPORT_SERVICES"] = ["DELAI_APUREMENT_IMPORT_SERVICES_J"],
        ["EXPORT_BIENS"] = ["DELAI_PAIEMENT_EXPORT_BIENS_J", "DELAI_APUREMENT_EXPORT_BIENS_J"],
        ["EXPORT_SERVICES"] = ["DELAI_APUREMENT_EXPORT_SERVICES_J"],
        ["TRANSFERT_CAPITAUX"] = ["DELAI_APUREMENT_TRANSFERT_CAPITAUX_J"],
    };

    public async Task<ExecutionDeclarationResponse> Handle(DeclarerExecutionCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        if (string.IsNullOrWhiteSpace(request.ReferenceABS) || string.IsNullOrWhiteSpace(request.ReferenceSWIFT))
        {
            throw new DomainException(422, ErrorResponseCode.VALEUR_HORS_PLAGE,
                "referenceABS et referenceSWIFT sont requis.");
        }

        var dossier = await db.Dossiers
            .Include(d => d.EtapesWorkflow)
            .FirstOrDefaultAsync(d => d.DossierId == command.DossierId, cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        if (dossier.StatutElectronique != nameof(StatutDossier.EN_EXECUTION_SWIFT))
        {
            throw new DomainException(422, ErrorResponseCode.STATUT_INVALIDE_POUR_ACTION,
                "Le dossier n'est pas en statut EN_EXECUTION_SWIFT — déclaration d'exécution impossible.");
        }

        if (request.MontantExecute > dossier.Montant)
        {
            throw new DomainException(422, ErrorResponseCode.VALEUR_HORS_PLAGE,
                "montantExecute ne peut pas dépasser le montant du dossier.");
        }

        var now = DateTime.UtcNow;
        if (request.DateExecution.UtcDateTime > now)
        {
            throw new DomainException(422, ErrorResponseCode.VALEUR_HORS_PLAGE,
                "dateExecution ne peut pas être dans le futur.");
        }

        if (!ClesDelaiParType.TryGetValue(dossier.TypeOperation, out var clesDelai))
        {
            throw new DomainException(422, ErrorResponseCode.VALEUR_HORS_PLAGE,
                $"Aucun délai d'apurement configuré dans ParametrageMetier pour le type d'opération {dossier.TypeOperation}.");
        }

        var delaisTrouves = await db.ParametresMetier
            .Where(p => clesDelai.Contains(p.Cle))
            .ToDictionaryAsync(p => p.Cle, p => int.Parse(p.Valeur), cancellationToken);

        if (clesDelai.Any(cle => !delaisTrouves.ContainsKey(cle)))
        {
            throw new DomainException(422, ErrorResponseCode.VALEUR_HORS_PLAGE,
                $"ParametrageMetier incomplet pour le type d'opération {dossier.TypeOperation}.");
        }

        var delaiJours = clesDelai.Sum(cle => delaisTrouves[cle]);

        var dateExecution = request.DateExecution.UtcDateTime;
        var dateEcheance = DateOnly.FromDateTime(dateExecution).AddDays(delaiJours);

        var statutAvant = dossier.StatutElectronique;

        dossier.ReferenceABS = request.ReferenceABS;
        dossier.ReferenceSWIFT = request.ReferenceSWIFT;
        dossier.NumeroAC = request.NumeroAC;
        dossier.CodeTRF = request.CodeTRF;
        dossier.DateExecution = dateExecution;
        dossier.MontantExecute = request.MontantExecute;
        dossier.DateEcheanceApurement = dateEcheance;
        // Rien n'est justifié encore, donc tout le montant exécuté reste à apurer.
        dossier.SoldeRestantApurement = request.MontantExecute;
        // EXECUTE n'est qu'un repli si Apurement/Archivage sont désactivés — sinon on avance vraiment.
        dossier.StatutElectronique = nameof(StatutDossier.EXECUTE);
        await WorkflowEngine.AvancerVersEtapeSuivante(db, dossier, cancellationToken);

        dossier.UpdatedAt = now;
        dossier.UpdatedBy = CurrentUser.Login;

        var etape = new EtapeWorkflow
        {
            DossierId = dossier.DossierId,
            NiveauValidation = WorkflowEngine.CodeEtapeCourante(dossier),
            StatutAvant = statutAvant,
            StatutApres = dossier.StatutElectronique,
            Action = nameof(ActionWorkflow.DECLARATION_EXECUTION),
            AgentLogin = CurrentUser.Login,
            DateAction = now,
            CreatedAt = now,
            CreatedBy = CurrentUser.Login,
        };
        dossier.EtapesWorkflow.Add(etape);
        JournalAuditWriter.EnregistrerTransition(db, dossier, etape);

        var dateAlerteJ14 = dateEcheance.AddDays(-14);
        var dateAlerteJ8 = dateEcheance.AddDays(-8);

        db.AlertesApurement.AddRange(
            new AlerteApurement
            {
                DossierId = dossier.DossierId,
                TypeAlerte = nameof(TypeAlerte.RELANCE_J14),
                JRestants = 14,
                DateAlerte = dateAlerteJ14.ToDateTime(TimeOnly.MinValue),
                CreatedAt = now,
                CreatedBy = CurrentUser.Login,
            },
            new AlerteApurement
            {
                DossierId = dossier.DossierId,
                TypeAlerte = nameof(TypeAlerte.MISE_EN_DEMEURE_J8),
                JRestants = 8,
                DateAlerte = dateAlerteJ8.ToDateTime(TimeOnly.MinValue),
                CreatedAt = now,
                CreatedBy = CurrentUser.Login,
            },
            new AlerteApurement
            {
                DossierId = dossier.DossierId,
                TypeAlerte = nameof(TypeAlerte.DEPASSEMENT_J0),
                JRestants = 0,
                DateAlerte = dateEcheance.ToDateTime(TimeOnly.MinValue),
                CreatedAt = now,
                CreatedBy = CurrentUser.Login,
            });

        await db.SaveChangesAsync(cancellationToken);

        return new ExecutionDeclarationResponse
        {
            DossierId = dossier.DossierId,
            ReferenceInterne = dossier.ReferenceInterne,
            StatutElectronique = Enum.Parse<StatutDossier>(dossier.StatutElectronique),
            ReferenceABS = dossier.ReferenceABS,
            ReferenceSWIFT = dossier.ReferenceSWIFT,
            NumeroAC = dossier.NumeroAC,
            CodeTRF = dossier.CodeTRF,
            DateExecution = dateExecution,
            DateEcheanceApurement = new DateTimeOffset(dateEcheance.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            AlertesJ14 = new DateTimeOffset(dateAlerteJ14.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
            AlertesJ8 = new DateTimeOffset(dateAlerteJ8.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
        };
    }
}
