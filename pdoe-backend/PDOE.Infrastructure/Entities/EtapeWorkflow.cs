namespace PDOE.Infrastructure.Entities;

/// Historique des transitions COMEX (INSERT ONLY). NiveauValidation référence WorkflowEtape.Code par convention, pas par FK, pour survivre au renommage d'une étape.
public class EtapeWorkflow
{
    public int EtapeId { get; set; }
    public int DossierId { get; set; }
    public string NiveauValidation { get; set; } = null!;
    public string StatutAvant { get; set; } = null!;
    public string StatutApres { get; set; } = null!;

    /// <summary>VALIDATION | REJET | ...</summary>
    public string Action { get; set; } = null!;
    public string? MotifRejet { get; set; }
    public string? ResponsableCorrection { get; set; }

    public string AgentLogin { get; set; } = null!;
    public DateTime DateAction { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;

    public Dossier Dossier { get; set; } = null!;
    public Utilisateur Agent { get; set; } = null!;
}
