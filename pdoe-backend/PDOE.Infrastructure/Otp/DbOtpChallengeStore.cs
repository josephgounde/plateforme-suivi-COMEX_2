using Microsoft.EntityFrameworkCore;
using PDOE.Infrastructure.Entities;

namespace PDOE.Infrastructure.Otp;

public class DbOtpChallengeStore(PdoeDbContext db) : IOtpChallengeStore
{
    public async Task<string> CreerAsync(OtpChallengeState etat, CancellationToken cancellationToken)
    {
        var otpToken = $"otp-{etat.LoginAD}-{Guid.NewGuid():N}";
        db.OtpChallenges.Add(new OtpChallenge
        {
            OtpToken = otpToken,
            LoginAD = etat.LoginAD,
            Code = etat.Code,
            NomComplet = etat.NomComplet,
            Email = etat.Email,
            Profil = etat.Profil,
            ExpiresAt = etat.ExpiresAt,
            Tentatives = etat.Tentatives,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
        return otpToken;
    }

    public async Task<OtpChallengeState?> RecupererAsync(string otpToken, CancellationToken cancellationToken)
    {
        var entry = await db.OtpChallenges.FirstOrDefaultAsync(o => o.OtpToken == otpToken, cancellationToken);
        if (entry is null)
            return null;

        // Purge paresseuse : un jeton expiré ne doit plus jamais être exploitable.
        if (entry.ExpiresAt <= DateTime.UtcNow)
        {
            db.OtpChallenges.Remove(entry);
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        return new OtpChallengeState(entry.LoginAD, entry.Code, entry.NomComplet, entry.Email, entry.Profil, entry.ExpiresAt, entry.Tentatives);
    }

    public async Task EnregistrerEchecAsync(string otpToken, CancellationToken cancellationToken)
    {
        var entry = await db.OtpChallenges.FirstOrDefaultAsync(o => o.OtpToken == otpToken, cancellationToken);
        if (entry is null)
            return;

        entry.Tentatives++;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task InvaliderAsync(string otpToken, CancellationToken cancellationToken)
    {
        var entry = await db.OtpChallenges.FirstOrDefaultAsync(o => o.OtpToken == otpToken, cancellationToken);
        if (entry is null)
            return;

        db.OtpChallenges.Remove(entry);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReinitialiserAsync(string otpToken, string nouveauCode, DateTime nouvelleExpiration, CancellationToken cancellationToken)
    {
        var entry = await db.OtpChallenges.FirstOrDefaultAsync(o => o.OtpToken == otpToken, cancellationToken);
        if (entry is null)
            return;

        entry.Code = nouveauCode;
        entry.ExpiresAt = nouvelleExpiration;
        entry.Tentatives = 0;
        await db.SaveChangesAsync(cancellationToken);
    }
}
