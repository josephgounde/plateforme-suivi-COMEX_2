namespace PDOE.Gateway.Common;

/// Cohérent avec les constantes équivalentes de auth.service.ts (mode mock) — même comportement perçu des deux côtés.
public static class OtpSettings
{
    public const int ValiditeSecondes = 180;
    public const int MaxTentatives = 3;
}
