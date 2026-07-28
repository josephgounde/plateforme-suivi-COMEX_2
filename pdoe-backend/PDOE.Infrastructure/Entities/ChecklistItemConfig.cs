namespace PDOE.Infrastructure.Entities;

/// <summary>Item de la checklist d'apurement (écran Apurement, SEQ-04).</summary>
public class ChecklistItemConfig
{
    public int ChecklistItemId { get; set; }
    public string Libelle { get; set; } = null!;

    /// <summary>Position 1..n, pilote l'ordre d'affichage.</summary>
    public int Ordre { get; set; }
    public bool Actif { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string UpdatedBy { get; set; } = null!;
}
