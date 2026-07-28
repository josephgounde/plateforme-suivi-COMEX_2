namespace PDOE.Infrastructure.Entities;

/// Trace de tout export généré depuis Reporting (réglementaire ET opérationnel — nom historique, gardé pour éviter un renommage de table). Pas de FK vers Dossiers : ça couvre une période, pas un dossier précis.
public class ExportReglementaire
{
    public int ExportReglementaireId { get; set; }

    /// <summary>REGLEMENTAIRE | OPERATIONNEL</summary>
    public string Categorie { get; set; } = null!;

    /// <summary>CRPI_DGI | CRPI_TRESOR | SITUATION_BCEAO | DOSSIERS_EN_RETARD | ACTIVITE_MENSUELLE</summary>
    public string TypeExport { get; set; } = null!;
    public DateOnly DateDebut { get; set; }
    public DateOnly DateFin { get; set; }
    public string NomFichier { get; set; } = null!;
    public string CheminFichier { get; set; } = null!;
    public string HashSHA256 { get; set; } = null!;
    public long TailleFichier { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Login de l'acteur ayant déclenché l'export.</summary>
    public string CreatedBy { get; set; } = null!;
}
