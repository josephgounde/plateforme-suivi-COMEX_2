using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Shared.Kernel.Common;
using PDOE.Workflow.API.Mapping;

namespace PDOE.Workflow.API.Features.ModifierEtapeConfig;

public class ModifierEtapeConfigHandler(PdoeDbContext db) : IRequestHandler<ModifierEtapeConfigCommand, EtapeWorkflowConfig>
{
    // Point d'entrée obligatoire du circuit — cf. PDOE_openapi.yaml.
    private const string CodeEtapeInitiation = "ETAPE_1_INITIATION";

    public async Task<EtapeWorkflowConfig> Handle(ModifierEtapeConfigCommand command, CancellationToken cancellationToken)
    {
        var etape = await db.WorkflowEtapes.FirstOrDefaultAsync(e => e.Code == command.Code, cancellationToken);
        if (etape is null)
            throw new DomainException(404, ErrorResponseCode.ETAPE_CONFIG_INTROUVABLE, "Étape introuvable.");

        var request = command.Request;

        if (request.Actif == false && etape.Code == CodeEtapeInitiation)
        {
            throw new DomainException(409, ErrorResponseCode.ETAPE_DESACTIVATION_REFUSEE,
                "Impossible de désactiver l'étape d'initiation — point d'entrée obligatoire du circuit.");
        }

        var estActivation = request.Actif == true && !etape.Actif;
        var estDesactivation = request.Actif == false && etape.Actif;

        if (request.Libelle is not null) etape.Libelle = request.Libelle;
        if (request.Actif is not null) etape.Actif = request.Actif.Value;
        if (request.Description is not null) etape.Description = request.Description;
        etape.UpdatedAt = DateTime.UtcNow;
        etape.UpdatedBy = CurrentUser.Login;

        // Miroir de mockModifierEtapeConfig côté frontend, qui journalise chaque changement du circuit.
        var (typeAction, libelleAction) = estActivation ? ("ETAPE_ACTIVEE", "Activation")
            : estDesactivation ? ("ETAPE_DESACTIVEE", "Désactivation")
            : ("ETAPE_MODIFIEE", "Modification");
        db.JournalAudit.Add(new JournalAudit
        {
            Categorie = "WORKFLOW",
            TypeAction = typeAction,
            Description = $"{libelleAction} de l'étape {etape.Code}.",
            EntiteType = "WorkflowEtapes",
            EntiteId = etape.Code,
            DateAction = etape.UpdatedAt,
            CreatedBy = CurrentUser.Login,
        });

        await db.SaveChangesAsync(cancellationToken);

        return etape.ToResponse();
    }
}
