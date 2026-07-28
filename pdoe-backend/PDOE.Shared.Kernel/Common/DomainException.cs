using PDOE.Api.Contracts;

namespace PDOE.Shared.Kernel.Common;

/// Erreur métier attendue (règle, précondition, ressource introuvable...). Levée par les handlers, traduite en ErrorResponse HTTP par le middleware global (Program.cs).
public class DomainException(int statusCode, ErrorResponseCode code, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public ErrorResponseCode Code { get; } = code;
}
