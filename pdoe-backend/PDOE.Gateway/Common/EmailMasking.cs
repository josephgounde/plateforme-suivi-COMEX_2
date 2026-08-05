namespace PDOE.Gateway.Common;

/// Port de la fonction masquerEmail équivalente dans auth.service.ts — même rendu des deux côtés (ex. "a**********@afbci.ci").
public static class EmailMasking
{
    public static string Masquer(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return email;

        var local = parts[0];
        var visible = local.Length > 0 ? local[..1] : "";
        var etoiles = new string('*', Math.Max(local.Length - 1, 3));
        return $"{visible}{etoiles}@{parts[1]}";
    }
}
