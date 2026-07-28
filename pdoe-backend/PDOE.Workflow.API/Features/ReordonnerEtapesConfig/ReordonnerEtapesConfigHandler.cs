using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Shared.Kernel.Common;
using PDOE.Workflow.API.Mapping;

namespace PDOE.Workflow.API.Features.ReordonnerEtapesConfig;

public class ReordonnerEtapesConfigHandler(PdoeDbContext db) : IRequestHandler<ReordonnerEtapesConfigCommand, List<EtapeWorkflowConfig>>
{
    public async Task<List<EtapeWorkflowConfig>> Handle(ReordonnerEtapesConfigCommand command, CancellationToken cancellationToken)
    {
        var nouvelOrdre = command.Request.Ordre.ToList();

        var etapes = await db.WorkflowEtapes.ToListAsync(cancellationToken);

        var codesExistants = etapes.Select(e => e.Code).ToHashSet();
        var codesDemandes = nouvelOrdre.ToHashSet();
        if (codesDemandes.Count != nouvelOrdre.Count || !codesExistants.SetEquals(codesDemandes))
        {
            throw new DomainException(422, ErrorResponseCode.ETAPE_ORDRE_INVALIDE,
                "La liste ne correspond pas exactement aux codes existants.");
        }

        var now = DateTime.UtcNow;
        var etapesParCode = etapes.ToDictionary(e => e.Code);

        // Deux passes pour éviter toute collision UNIQUE(Ordre) : d'abord des valeurs temporaires hors de portée, puis les positions finales.
        var decalageTemporaire = nouvelOrdre.Count;
        foreach (var (code, index) in nouvelOrdre.Select((code, i) => (code, i)))
        {
            etapesParCode[code].Ordre = decalageTemporaire + index + 1;
        }
        await db.SaveChangesAsync(cancellationToken);

        foreach (var (code, index) in nouvelOrdre.Select((code, i) => (code, i)))
        {
            var etape = etapesParCode[code];
            etape.Ordre = index + 1;
            etape.UpdatedAt = now;
            etape.UpdatedBy = CurrentUser.Login;
        }

        // Miroir de mockReordonnerEtapes côté frontend, qui journalise chaque changement du circuit.
        db.JournalAudit.Add(new JournalAudit
        {
            Categorie = "WORKFLOW",
            TypeAction = "ETAPES_REORDONNEES",
            Description = $"Réordonnancement du circuit : {string.Join(" → ", nouvelOrdre)}.",
            EntiteType = "WorkflowEtapes",
            DateAction = now,
            CreatedBy = CurrentUser.Login,
        });

        await db.SaveChangesAsync(cancellationToken);

        return etapes.OrderBy(e => e.Ordre).Select(e => e.ToResponse()).ToList();
    }
}
