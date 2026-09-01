using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using PDOE.Infrastructure.Ldap;

namespace PDOE.Gateway.Ldap;

/// Passerelle HTTP interne AFBCI vers l'AD (pas un bind LDAP direct — cf. HttpNotificationSender pour le même
/// principe côté messagerie). "passphrase" = SHA256(password) en hexadécimal minuscule, exigé par le service en
/// plus du mot de passe en clair (vérification côté serveur) — sans ça l'utilisateur ne peut pas s'authentifier.
public class HttpLdapAuthenticator(HttpClient http) : ILdapAuthenticator
{
    public async Task<LdapBindResult> AuthentifierAsync(string login, string password, CancellationToken cancellationToken)
    {
        try
        {
            var response = await http.PostAsJsonAsync(
                http.BaseAddress,
                new
                {
                    username = login,
                    password,
                    passphrase = HasherMotDePasse(password),
                },
                cancellationToken);

            if (response.IsSuccessStatusCode)
                return new LdapBindResult(true, null);

            
            // LDAP_UNAVAILABLE pour les pannes serveur, INVALID_CREDENTIALS générique sinon, à affiner
            // une fois observé en conditions réelles (même démarche que pour HttpNotificationSender).
            var codeErreur = (int)response.StatusCode >= 500 ? "LDAP_UNAVAILABLE" : "INVALID_CREDENTIALS";
            return new LdapBindResult(false, codeErreur);
            
        }
        // TaskCanceledException : déclenchée par client.Timeout (15s, cf. Program.cs) — sans ce catch, un délai
        // dépassé remontait comme une exception non gérée (500 brut) au lieu du message LDAP_UNAVAILABLE attendu.
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new LdapBindResult(false, "LDAP_UNAVAILABLE");
        }
    }

    private static string HasherMotDePasse(string password)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
