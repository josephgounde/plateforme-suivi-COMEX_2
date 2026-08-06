using System.Net.Http.Json;
using PDOE.Api.Contracts;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Infrastructure.Cbs;

/// Client HTTP réel vers ABS2000. Chemins alignés sur la façade REST déjà exposée par PDOE (TauxChangeController /
/// ClientsController), à confirmer/ajuster contre la vraie documentation ABS2000 une fois l'accès fourni — même
/// principe que HttpLdapAuthenticator (BaseUrl configurable, jamais d'appel direct depuis ce process avant que
/// l'utilisateur n'ait renseigné Cbs:BaseUrl lui-même).
public class HttpCbsClient(HttpClient http) : ICbsClient
{
    public async Task<TauxChangeResult> ObtenirTauxChangeAsync(string devise, string versDevise, CancellationToken cancellationToken)
    {
        try
        {
            var reponse = await http.GetFromJsonAsync<TauxChangeResult>(
                $"taux-change?devise={devise}&versDevise={versDevise}", cancellationToken);
            return reponse ?? throw ReponseVide();
        }
        catch (HttpRequestException ex)
        {
            throw Indisponible(ex);
        }
    }

    public async Task<SoldeClientResult> ObtenirSoldeClientAsync(string numCompte, CancellationToken cancellationToken)
    {
        try
        {
            var reponse = await http.GetFromJsonAsync<SoldeClientResult>(
                $"clients/{numCompte}/solde", cancellationToken);
            return reponse ?? throw ReponseVide();
        }
        catch (HttpRequestException ex)
        {
            throw Indisponible(ex);
        }
    }

    public async Task<SignatureVerificationResult> VerifierSignatureAsync(string numCompte, ModeVerificationSignature mode, CancellationToken cancellationToken)
    {
        try
        {
            var reponse = await http.GetFromJsonAsync<SignatureVerificationResult>(
                $"clients/{numCompte}/verifier-signature?mode={mode}", cancellationToken);
            return reponse ?? throw ReponseVide();
        }
        catch (HttpRequestException ex)
        {
            throw Indisponible(ex);
        }
    }

    private static DomainException Indisponible(HttpRequestException ex) =>
        new(502, ErrorResponseCode.ABS_INDISPONIBLE, $"ABS2000 indisponible ou en erreur : {ex.Message}");

    private static DomainException ReponseVide() =>
        new(502, ErrorResponseCode.ABS_INDISPONIBLE, "Réponse ABS2000 vide.");
}
