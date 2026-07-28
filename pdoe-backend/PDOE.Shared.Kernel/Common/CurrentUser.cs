namespace PDOE.Shared.Kernel.Common;

/// Placeholder tant que PDOE.Gateway (LDAP+JWT) n'est pas branché, à remplacer par HttpContext.User. Doit être un LoginAD réel (FK vers Utilisateurs), d'où le seed "admin.dsiri".
public static class CurrentUser
{
    public const string Login = "admin.dsiri";
}
