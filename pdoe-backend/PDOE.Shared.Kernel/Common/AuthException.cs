using PDOE.Api.Contracts;

namespace PDOE.Shared.Kernel.Common;

/// Erreur d'authentification (login/OTP) — traduite en AuthErrorResponse par le middleware global (Program.cs).
/// Format distinct de DomainException/ErrorResponse : cf. commentaire sur AuthErrorResponse (jeux de codes disjoints).
public class AuthException(int statusCode, AuthErrorResponseCode code, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public AuthErrorResponseCode Code { get; } = code;
}
