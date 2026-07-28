namespace PDOE.Infrastructure.Entities;

public class PaiementPartiel
{
    public int PaiementId { get; set; }
    public int DossierId { get; set; }
    public decimal MontantPaiement { get; set; }
    public string Devise { get; set; } = null!;
    public DateOnly DatePaiement { get; set; }
    public string ReferencePaiement { get; set; } = null!;
    public decimal SoldeRestant { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;

    public Dossier Dossier { get; set; } = null!;
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
