using PDOE.Api.Contracts;
using PDOE.Infrastructure.Entities;

namespace PDOE.Workflow.API.Mapping;

/// Mapper dupliqué à dessein depuis PDOE.Dossiers.API — les modules ne se référencent jamais entre eux.
public static class WorkflowMappingExtensions
{
    public static EtapeWorkflowResponse ToResponse(this EtapeWorkflow e) => new()
    {
        EtapeId = e.EtapeId,
        NiveauValidation = e.NiveauValidation,
        StatutAvant = Enum.Parse<StatutDossier>(e.StatutAvant),
        StatutApres = Enum.Parse<StatutDossier>(e.StatutApres),
        Action = Enum.Parse<ActionWorkflow>(e.Action),
        MotifRejet = e.MotifRejet,
        ResponsableCorrection = e.ResponsableCorrection,
        AgentLogin = e.AgentLogin,
        DateAction = e.DateAction,
    };

    public static EtapeWorkflowConfig ToResponse(this WorkflowEtape w) => new()
    {
        EtapeConfigId = w.EtapeConfigId,
        Code = w.Code,
        Libelle = w.Libelle,
        Ordre = w.Ordre,
        Actif = w.Actif,
        TypeEtape = Enum.Parse<TypeEtapeWorkflow>(w.TypeEtape),
        Description = w.Description,
        CreatedAt = w.CreatedAt,
        UpdatedAt = w.UpdatedAt,
        UpdatedBy = w.UpdatedBy,
    };
}
