using PDOE.Api.Contracts;

namespace PDOE.Infrastructure.Cbs;

/// Abstraction de l'accès ABS2000 (taux de change, solde, signature) — lecture seule côté PDOE.
/// En dev/tant que l'accès réel n'est pas fourni : MockCbsClient. Une fois l'accès obtenu : HttpCbsClient
/// (même principe de bascule que ILdapAuthenticator/INotificationSender, piloté par Cbs:BypassValidation).
/// Pas de ValiderSignatureVisuelle ici : ABS2000 ne sait que dire si une signature existe, pas la restituer —
/// la confirmation visuelle se fait par l'agent hors PDOE (connexion directe à ABS2000), voir
/// PDOE.CBS.Integration/Features/ValiderSignatureVisuelle, qui n'appelle pas ce client.
public interface ICbsClient
{
    Task<TauxChangeResult> ObtenirTauxChangeAsync(string devise, string versDevise, CancellationToken cancellationToken);

    Task<SoldeClientResult> ObtenirSoldeClientAsync(string numCompte, CancellationToken cancellationToken);

    Task<SignatureVerificationResult> VerifierSignatureAsync(string numCompte, ModeVerificationSignature mode, CancellationToken cancellationToken);
}
