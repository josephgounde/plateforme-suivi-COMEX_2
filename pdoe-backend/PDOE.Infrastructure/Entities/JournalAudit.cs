namespace PDOE.Infrastructure.Entities;

/// Audit admin/sécurité, pas le cycle de vie des dossiers (voir EtapeWorkflow pour ça). Pas de FK vers Utilisateur : la ligne doit survivre à la suppression du compte.
public class JournalAudit
{
    public int JournalAuditId { get; set; }

    /// <summary>AUTHENTIFICATION | UTILISATEUR | PARAMETRAGE | WORKFLOW | REPORTING</summary>
    public string Categorie { get; set; } = null!;

    /// <summary>CONNEXION_REUSSIE, CONNEXION_ECHEC, DECONNEXION, UTILISATEUR_CREE, EXPORT_RAPPORT, ...</summary>
    public string TypeAction { get; set; } = null!;
    public string Description { get; set; } = null!;

    /// <summary>'Utilisateur' / 'ParametrageMetier' / 'WorkflowEtapes' / 'ExportReglementaire' / null pour un événement d'authentification.</summary>
    public string? EntiteType { get; set; }
    public string? EntiteId { get; set; }
    public bool Succes { get; set; } = true;
    public DateTime DateAction { get; set; }

    /// <summary>Login de l'acteur, ou le login tenté en cas d'échec de connexion.</summary>
    public string CreatedBy { get; set; } = null!;
}
