namespace PDOE.Infrastructure.Entities;

/// <summary>Portefeuille automatique Gestionnaire ↔ compte client.</summary>
public class GestionnaireClient
{
    public int GestionnaireClientId { get; set; }
    public string GestionnaireLogin { get; set; } = null!;
    public string NumCompte { get; set; } = null!;
    public DateTime DateAffectation { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;

    public Utilisateur Gestionnaire { get; set; } = null!;
}
