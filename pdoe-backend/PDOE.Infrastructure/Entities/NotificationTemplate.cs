namespace PDOE.Infrastructure.Entities;

/// Un modèle par TypeEvenement (clé primaire, pas d'IDENTITY) — la liste d'événements est fixée dans le code, seul le contenu est modifiable.
public class NotificationTemplate
{
    public string TypeEvenement { get; set; } = null!;
    public string Libelle { get; set; } = null!;
    public string Corps { get; set; } = null!;

    /// <summary>SMS | EMAIL | SMS_ET_EMAIL</summary>
    public string CanalDefaut { get; set; } = "EMAIL";

    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = null!;
}
