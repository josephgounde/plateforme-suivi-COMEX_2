namespace PDOE.Infrastructure.Entities;

public class Utilisateur
{
    public int UtilisateurId { get; set; }

    /// <summary>Clé pivot extraite du JWT — login Active Directory.</summary>
    public string LoginAD { get; set; } = null!;
    public string Nom { get; set; } = null!;
    public string Prenom { get; set; } = null!;
    public string Email { get; set; } = null!;

    /// <summary>AGENT_ACCUEIL | GESTIONNAIRE | AGENT_COMEX | TRESORERIE | DIRECTION | ADMIN_DSIRI | SUPER_ADMIN</summary>
    public string Profil { get; set; } = null!;
    public bool EstActif { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string UpdatedBy { get; set; } = null!;

    public ICollection<GestionnaireClient> Portefeuille { get; set; } = new List<GestionnaireClient>();
    public ICollection<Dossier> DossiersAssignes { get; set; } = new List<Dossier>();
}
