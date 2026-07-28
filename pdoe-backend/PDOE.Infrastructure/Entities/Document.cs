namespace PDOE.Infrastructure.Entities;

public class Document
{
    public int DocumentId { get; set; }
    public int DossierId { get; set; }
    public int? PaiementId { get; set; }
    public string TypeDocument { get; set; } = null!;
    /// <summary>N° de référence du document lui-même (ex : n° de facture, n° d'AC, n° de formulaire de change) — distinct de NomFichier (nom système du fichier uploadé).</summary>
    public string? ReferenceDocument { get; set; }
    public string NomFichier { get; set; } = null!;
    public string CheminIIS { get; set; } = null!;
    public string HashSHA256 { get; set; } = null!;
    public long TailleFichier { get; set; }
    public bool EstObligatoire { get; set; }
    public bool EstValide { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;

    public Dossier Dossier { get; set; } = null!;
    public PaiementPartiel? Paiement { get; set; }
}
