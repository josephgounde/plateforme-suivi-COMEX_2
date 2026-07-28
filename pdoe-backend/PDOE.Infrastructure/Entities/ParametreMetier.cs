namespace PDOE.Infrastructure.Entities;

public class ParametreMetier
{
    public int ParametreId { get; set; }
    public string Cle { get; set; } = null!;
    public string Valeur { get; set; } = null!;
    public string Unite { get; set; } = null!;
    public string Description { get; set; } = null!;
    public bool ModifiableUI { get; set; } = true;
    public string? ValeurMin { get; set; }
    public string? ValeurMax { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string UpdatedBy { get; set; } = null!;
}
