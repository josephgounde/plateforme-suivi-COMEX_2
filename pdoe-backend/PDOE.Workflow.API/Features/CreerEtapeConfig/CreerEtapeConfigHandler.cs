using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Shared.Kernel.Common;
using PDOE.Workflow.API.Mapping;

namespace PDOE.Workflow.API.Features.CreerEtapeConfig;

public class CreerEtapeConfigHandler(PdoeDbContext db) : IRequestHandler<CreerEtapeConfigCommand, EtapeWorkflowConfig>
{
    public async Task<EtapeWorkflowConfig> Handle(CreerEtapeConfigCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        var codeDejaUtilise = await db.WorkflowEtapes.AnyAsync(e => e.Code == request.Code, cancellationToken);
        if (codeDejaUtilise)
        {
            throw new DomainException(409, ErrorResponseCode.ETAPE_CODE_DEJA_UTILISE,
                "Ce code d'étape est déjà utilisé.");
        }

        // Décale +1 tout ce qui suit — ExecuteUpdateAsync fait un UPDATE ensembliste, donc pas de collision transitoire sur UNIQUE(Ordre).
        await db.WorkflowEtapes
            .Where(e => e.Ordre >= request.Ordre)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Ordre, e => e.Ordre + 1), cancellationToken);

        var now = DateTime.UtcNow;
        var etape = new WorkflowEtape
        {
            Code = request.Code,
            Libelle = request.Libelle,
            Ordre = request.Ordre,
            Actif = true,
            // GENERIQUE par défaut — un typeEtape spécialisé (COMEX...) n'a de sens que pour les 7 étapes historiques déjà seedées.
            TypeEtape = (request.TypeEtape ?? TypeEtapeWorkflow.GENERIQUE).ToString(),
            Description = request.Description,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = CurrentUser.Login,
            UpdatedBy = CurrentUser.Login,
        };

        db.WorkflowEtapes.Add(etape);

        // Miroir de mockCreerEtapeConfig côté frontend, qui journalise chaque changement du circuit.
        db.JournalAudit.Add(new JournalAudit
        {
            Categorie = "WORKFLOW",
            TypeAction = "ETAPE_CREEE",
            Description = $"Création de l'étape {etape.Code} ({etape.Libelle}), position {etape.Ordre}.",
            EntiteType = "WorkflowEtapes",
            EntiteId = etape.Code,
            DateAction = now,
            CreatedBy = CurrentUser.Login,
        });

        await db.SaveChangesAsync(cancellationToken);

        return etape.ToResponse();
    }
}
