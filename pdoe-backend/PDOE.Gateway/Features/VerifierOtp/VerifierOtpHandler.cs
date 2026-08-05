using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Gateway.Common;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Infrastructure.Otp;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Gateway.Features.VerifierOtp;

/// Étape 2/2 : aucune session n'existe avant un code valide — cf. description OpenAPI.
public class VerifierOtpHandler(PdoeDbContext db, IOtpChallengeStore otpStore, IJwtTokenGenerator jwt)
    : IRequestHandler<VerifierOtpCommand, SessionResponse>
{
    public async Task<SessionResponse> Handle(VerifierOtpCommand command, CancellationToken cancellationToken)
    {
        var otpToken = command.Request.OtpToken;
        var etat = await otpStore.RecupererAsync(otpToken, cancellationToken);
        if (etat is null)
            throw new AuthException(410, AuthErrorResponseCode.OTP_EXPIRED, "Code expiré.");

        if (command.Request.Code != etat.Code)
        {
            await otpStore.EnregistrerEchecAsync(otpToken, cancellationToken);

            if (etat.Tentatives + 1 >= OtpSettings.MaxTentatives)
            {
                await otpStore.InvaliderAsync(otpToken, cancellationToken);
                await EnregistrerAudit(etat.LoginAD, "CONNEXION_ECHEC", "Trop de tentatives OTP échouées — jeton invalidé.", false, cancellationToken);
                throw new AuthException(429, AuthErrorResponseCode.OTP_MAX_TENTATIVES, "Trop de tentatives échouées.");
            }

            await EnregistrerAudit(etat.LoginAD, "CONNEXION_ECHEC", "Code OTP incorrect.", false, cancellationToken);
            throw new AuthException(401, AuthErrorResponseCode.OTP_INVALID, "Code incorrect.");
        }

        // Revérifié ici (pas seulement au login) : le compte a pu être désactivé pendant la fenêtre OTP.
        var utilisateur = await db.Utilisateurs.FirstOrDefaultAsync(u => u.LoginAD == etat.LoginAD, cancellationToken);
        if (utilisateur is null || !utilisateur.EstActif)
        {
            await otpStore.InvaliderAsync(otpToken, cancellationToken);
            throw new AuthException(403, AuthErrorResponseCode.ACCOUNT_DISABLED, "Compte désactivé. Contactez votre administrateur.");
        }

        await otpStore.InvaliderAsync(otpToken, cancellationToken);
        var (token, expiresAt) = jwt.Generer(utilisateur);

        await EnregistrerAudit(utilisateur.LoginAD, "CONNEXION_REUSSIE",
            $"Connexion réussie — {utilisateur.Prenom} {utilisateur.Nom} ({utilisateur.Profil}).", true, cancellationToken);

        return new SessionResponse
        {
            Login = utilisateur.LoginAD,
            NomComplet = $"{utilisateur.Prenom} {utilisateur.Nom}",
            Email = utilisateur.Email,
            Profil = Enum.Parse<ProfilUtilisateur>(utilisateur.Profil),
            Token = token,
            ExpiresAt = expiresAt,
        };
    }

    private async Task EnregistrerAudit(string login, string typeAction, string description, bool succes, CancellationToken cancellationToken)
    {
        db.JournalAudit.Add(new JournalAudit
        {
            Categorie = "AUTHENTIFICATION",
            TypeAction = typeAction,
            Description = description,
            Succes = succes,
            DateAction = DateTime.UtcNow,
            CreatedBy = login,
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
