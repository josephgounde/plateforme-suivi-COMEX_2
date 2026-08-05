namespace PDOE.Infrastructure.Otp;

/// otpToken n'apparaît dans aucun schéma de lecture, seulement en aller-retour login → otp/verifier|renvoyer
/// (cf. OpenAPI) — mais l'état sous-jacent est persisté (dbo.OtpChallenges), pas gardé en mémoire du process.
public interface IOtpChallengeStore
{
    Task<string> CreerAsync(OtpChallengeState etat, CancellationToken cancellationToken);
    Task<OtpChallengeState?> RecupererAsync(string otpToken, CancellationToken cancellationToken);
    Task EnregistrerEchecAsync(string otpToken, CancellationToken cancellationToken);
    Task InvaliderAsync(string otpToken, CancellationToken cancellationToken);
    Task ReinitialiserAsync(string otpToken, string nouveauCode, DateTime nouvelleExpiration, CancellationToken cancellationToken);
}

public record OtpChallengeState(
    string LoginAD,
    string Code,
    string NomComplet,
    string Email,
    string Profil,
    DateTime ExpiresAt,
    int Tentatives);
