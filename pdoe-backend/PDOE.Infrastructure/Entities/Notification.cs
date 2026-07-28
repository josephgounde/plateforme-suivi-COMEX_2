namespace PDOE.Infrastructure.Entities;

public class Notification
{
    public int NotificationId { get; set; }
    public int? DossierId { get; set; }
    public string TypeEvenement { get; set; } = null!;

    /// <summary>SMS | EMAIL | SMS_ET_EMAIL</summary>
    public string Canal { get; set; } = null!;
    public string Destinataire { get; set; } = null!;
    public string? Sujet { get; set; }
    public string Corps { get; set; } = null!;
    public string? MessageIdGateway { get; set; }
    public string Statut { get; set; } = "EN_ATTENTE";
    public string? CodeErreur { get; set; }
    public int NbTentatives { get; set; }
    public DateTime? DateEnvoi { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;

    public Dossier? Dossier { get; set; }
}
