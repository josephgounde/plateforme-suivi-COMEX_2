using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace PDOE.Shared.Kernel.Common;

/// Pont statique vers HttpContext.User — configuré une fois au démarrage (Program.cs) via Configure(), pour
/// que chaque CurrentUser.Login existant dans les handlers reste inchangé plutôt que de propager IHttpContextAccessor
/// partout. Retombe sur "SYSTEM" hors contexte HTTP (jobs planifiés — cf. AlerteApurementSchedulerService/NotificationRetryService).
public static class CurrentUser
{
    private static IHttpContextAccessor? _accessor;

    public static void Configure(IHttpContextAccessor accessor) => _accessor = accessor;

    public static string Login =>
        _accessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "SYSTEM";
}
