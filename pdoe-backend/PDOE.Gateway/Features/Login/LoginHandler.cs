using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Gateway.Common;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Infrastructure.Ldap;
using PDOE.Infrastructure.Notifications;
using PDOE.Infrastructure.Otp;
using PDOE.Shared.Kernel.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PDOE.Gateway.Features.Login;

/// Étape 1/2 : bind LDAP (identité) puis résolution du profil PDOE via Utilisateurs — jamais l'inverse
/// Auth ne fait que vérifier l'identité, jamais le profil.
public class LoginHandler(
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<LoginHandler> logger,
    PdoeDbContext db,
    ILdapAuthenticator ldap,
    IOtpChallengeStore otpStore,
    INotificationSender sender) : IRequestHandler<LoginCommand, OtpChallengeResponse>
{
    public async Task<OtpChallengeResponse> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var login = command.Request.Login;
        var bind = await ldap.AuthentifierAsync(login, command.Request.Password, cancellationToken);

        if (!bind.Succes)
        {
            await EnregistrerEchec(login, bind.CodeErreur ?? "INVALID_CREDENTIALS", cancellationToken);
            throw MapperEchecLdap(bind.CodeErreur);
        }

        var utilisateur = await db.Utilisateurs.FirstOrDefaultAsync(u => u.LoginAD == login, cancellationToken);
        if (utilisateur is null)
        {
            await EnregistrerEchec(login, "NO_PROFILE_MAPPED", cancellationToken);
            throw new AuthException(403, AuthErrorResponseCode.NO_PROFILE_MAPPED,
                "Aucun profil PGSA-COMEX associé à ce compte. Contactez l'administrateur DSIRI.");
        }

        if (!utilisateur.EstActif)
        {
            await EnregistrerEchec(login, "ACCOUNT_DISABLED", cancellationToken);
            throw new AuthException(403, AuthErrorResponseCode.ACCOUNT_DISABLED, "Compte désactivé. Contactez votre administrateur.");
        }

        var code = GenererCode();

        if (environment.IsDevelopment() && configuration.GetValue<bool>("Otp:LogCodeInConsole"))
            logger.LogWarning("[DEV] Code OTP pour {Login} : {Code}", login, code);

        var expiresAt = DateTime.UtcNow.AddSeconds(OtpSettings.ValiditeSecondes);
        var otpToken = await otpStore.CreerAsync(new OtpChallengeState(
            utilisateur.LoginAD, code, $"{utilisateur.Prenom} {utilisateur.Nom}", utilisateur.Email, utilisateur.Profil, expiresAt, 0),
            cancellationToken);

        var resultatEnvoi = await sender.EnvoyerAsync("EMAIL", utilisateur.Email, "Code de vérification PGSA-COMEX",
            $"Votre code de vérification PGSA-COMEX est : {code}. Il est valable {OtpSettings.ValiditeSecondes / 60} minutes.",
            cancellationToken);

        var destinataireMasque = EmailMasking.Masquer(utilisateur.Email);
        await EnregistrerAudit(login,
            resultatEnvoi.Succes ? "OTP_ENVOYE" : "OTP_ENVOI_ECHEC",
            resultatEnvoi.Succes
                ? $"Code OTP envoyé par email à {destinataireMasque}."
                : $"Échec de l'envoi du code OTP à {destinataireMasque} — {resultatEnvoi.CodeErreur}.",
            resultatEnvoi.Succes, cancellationToken);

        return new OtpChallengeResponse
        {
            OtpToken = otpToken,
            Canal = CanalNotification.EMAIL,
            DestinataireMasque = destinataireMasque,
            ExpiresInSeconds = OtpSettings.ValiditeSecondes,
        };
    }

    private Task EnregistrerEchec(string login, string code, CancellationToken cancellationToken) =>
        EnregistrerAudit(login, "CONNEXION_ECHEC", $"Échec de connexion pour \"{login}\" ({code}).", false, cancellationToken);

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

    private static AuthException MapperEchecLdap(string? code) => code switch
    {
        "ACCOUNT_LOCKED" => new AuthException(403, AuthErrorResponseCode.ACCOUNT_LOCKED,
            "Compte verrouillé après plusieurs tentatives échouées. Contactez le support DSIRI."),
        "PASSWORD_EXPIRED" => new AuthException(403, AuthErrorResponseCode.PASSWORD_EXPIRED,
            "Mot de passe expiré — merci de le réinitialiser via le portail AD."),
        "ACCOUNT_DISABLED" => new AuthException(403, AuthErrorResponseCode.ACCOUNT_DISABLED,
            "Compte désactivé. Contactez votre administrateur."),
        "LDAP_UNAVAILABLE" => new AuthException(503, AuthErrorResponseCode.LDAP_UNAVAILABLE,
            "Service d'authentification indisponible. Réessayez dans quelques instants."),
        _ => new AuthException(401, AuthErrorResponseCode.INVALID_CREDENTIALS, "Identifiants invalides."),
    };

    private static string GenererCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
}
