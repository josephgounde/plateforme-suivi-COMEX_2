namespace PDOE.Infrastructure.Entities;

/// Étape configurable du circuit, Ordre pilote le routage réel. Pas de suppression physique, on désactive (Actif = false).
public class WorkflowEtape
{
    public int EtapeConfigId { get; set; }

    /// <summary>ex 'ETAPE_3_COMEX' (historique) ou 'ETAPE_8_CONFORMITE' (custom).</summary>
    public string Code { get; set; } = null!;
    public string Libelle { get; set; } = null!;

    /// <summary>Position 1..n, pilote le routage réel.</summary>
    public int Ordre { get; set; }
    public bool Actif { get; set; } = true;

    /// <summary>GESTIONNAIRE | COMEX | TRESORERIE | EXECUTION | APUREMENT | GENERIQUE</summary>
    public string TypeEtape { get; set; } = null!;
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string UpdatedBy { get; set; } = null!;
}
