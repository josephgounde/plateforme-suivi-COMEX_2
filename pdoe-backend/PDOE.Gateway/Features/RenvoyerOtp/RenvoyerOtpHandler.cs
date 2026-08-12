using System.Security.Cryptography;
using MediatR;
using PDOE.Api.Contracts;
using PDOE.Gateway.Common;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Infrastructure.Notifications;
using PDOE.Infrastructure.Otp;
using PDOE.Shared.Kernel.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace PDOE.Gateway.Features.RenvoyerOtp;

/// Réinitialise l'horloge/compteur du même otpToken — ne redémarre pas toute la vérification (cf. OpenAPI).
public class RenvoyerOtpHandler(PdoeDbContext db, IOtpChallengeStore otpStore, INotificationSender sender, IConfiguration configuration, IHostEnvironment environment, ILogger<RenvoyerOtpHandler> logger)

    : IRequestHandler<RenvoyerOtpCommand, OtpChallengeResponse>
{
    public async Task<OtpChallengeResponse> Handle(RenvoyerOtpCommand command, CancellationToken cancellationToken)
    {
        var otpToken = command.Request.OtpToken;
        var etat = await otpStore.RecupererAsync(otpToken, cancellationToken);
        if (etat is null)
            throw new AuthException(410, AuthErrorResponseCode.OTP_EXPIRED, "Code expiré.");

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        if (environment.IsDevelopment() && configuration.GetValue<bool>("Otp:LogCodeInConsole"))
            logger.LogWarning("[DEV] Code OTP pour {Login} : {Code}", etat.LoginAD, code);

        var expiresAt = DateTime.UtcNow.AddSeconds(OtpSettings.ValiditeSecondes);
        await otpStore.ReinitialiserAsync(otpToken, code, expiresAt, cancellationToken);

        var resultatEnvoi = await sender.EnvoyerAsync("EMAIL", etat.Email, "Code de vérification PGSA-COMEX",
            $"Votre code de vérification PGSA-COMEX est : {code}. Il est valable {OtpSettings.ValiditeSecondes / 60} minutes.",
            cancellationToken);

        var destinataireMasque = EmailMasking.Masquer(etat.Email);
        db.JournalAudit.Add(new JournalAudit
        {
            Categorie = "AUTHENTIFICATION",
            TypeAction = resultatEnvoi.Succes ? "OTP_ENVOYE" : "OTP_ENVOI_ECHEC",
            Description = resultatEnvoi.Succes
                ? $"Code OTP renvoyé par email à {destinataireMasque}."
                : $"Échec du renvoi du code OTP à {destinataireMasque} — {resultatEnvoi.CodeErreur}.",
            Succes = resultatEnvoi.Succes,
            DateAction = DateTime.UtcNow,
            CreatedBy = etat.LoginAD,
        });
        await db.SaveChangesAsync(cancellationToken);

        return new OtpChallengeResponse
        {
            OtpToken = otpToken,
            Canal = CanalNotification.EMAIL,
            DestinataireMasque = destinataireMasque,
            ExpiresInSeconds = OtpSettings.ValiditeSecondes,
        };
    }
}
