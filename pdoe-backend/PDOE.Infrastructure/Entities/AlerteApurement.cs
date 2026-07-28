namespace PDOE.Infrastructure.Entities;

public class AlerteApurement
{
    public int AlerteId { get; set; }
    public int DossierId { get; set; }

    /// <summary>J14 | J8 | J0 (cf. seuils ParametrageMetier)</summary>
    public string TypeAlerte { get; set; } = null!;
    public int JRestants { get; set; }
    public DateTime DateAlerte { get; set; }
    public bool Envoye { get; set; }
    public DateTime? DateEnvoi { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;

    public Dossier Dossier { get; set; } = null!;
}
