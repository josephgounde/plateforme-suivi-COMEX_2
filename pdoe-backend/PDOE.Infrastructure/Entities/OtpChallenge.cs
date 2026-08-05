namespace PDOE.Infrastructure.Entities;

/// État transitoire d'un défi OTP (login → otp/verifier|renvoyer). Remplace l'ancien stockage
/// en mémoire (perdu à chaque redémarrage, incompatible avec plusieurs instances) — même
/// durée de vie courte (quelques minutes), juste persistée.
public class OtpChallenge
{
    public string OtpToken { get; set; } = null!;
    public string LoginAD { get; set; } = null!;
    public string Code { get; set; } = null!;
    public string NomComplet { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Profil { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public int Tentatives { get; set; }
    public DateTime CreatedAt { get; set; }
}
