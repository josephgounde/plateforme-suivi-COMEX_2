using PDOE.Api.Contracts;

namespace PDOE.Infrastructure.Cbs;

/// Abstraction de l'accès ABS2000 (taux de change, solde, signature) — lecture seule côté PDOE.
/// En dev/tant que l'accès réel n'est pas fourni : MockCbsClient. Une fois l'accès obtenu : HttpCbsClient
/// (même principe de bascule que ILdapAuthenticator/INotificationSender, piloté par Cbs:BypassValidation).
public interface ICbsClient
{
    Task<TauxChangeResult> ObtenirTauxChangeAsync(string devise, string versDevise, CancellationToken cancellationToken);

    Task<SoldeClientResult> ObtenirSoldeClientAsync(string numCompte, CancellationToken cancellationToken);

    Task<SignatureVerificationResult> VerifierSignatureAsync(string numCompte, ModeVerificationSignature mode, CancellationToken cancellationToken);

    Task<bool> ValiderSignatureVisuelleAsync(string numCompte, string initialesAgent, CancellationToken cancellationToken);
}
