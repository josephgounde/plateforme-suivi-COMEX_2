namespace PDOE.Infrastructure.Ldap;

/// Abstraction du bind LDAP (Active Directory AFBCI) — vérifie l'identité uniquement, jamais le profil PDOE
/// (résolu séparément via Utilisateurs). Voir RealLdapAuthenticator pour l'implémentation.
public interface ILdapAuthenticator
{
    Task<LdapBindResult> AuthentifierAsync(string login, string password, CancellationToken cancellationToken);
}

/// CodeErreur ∈ INVALID_CREDENTIALS | ACCOUNT_LOCKED | PASSWORD_EXPIRED | LDAP_UNAVAILABLE (valeurs de AuthErrorResponseCode).
public record LdapBindResult(bool Succes, string? CodeErreur);
