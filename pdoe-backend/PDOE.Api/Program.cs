using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PDOE.Api.Contracts;
using PDOE.Gateway.Common;
using PDOE.Gateway.Ldap;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Archive;
using PDOE.Infrastructure.Cbs;
using PDOE.Infrastructure.Ldap;
using PDOE.Infrastructure.Notifications;
using PDOE.Infrastructure.Otp;
using PDOE.Infrastructure.Storage;
using PDOE.Shared.Kernel.Common;
using Serilog;

// Sans ça, [Range(typeof(decimal), "0.0001", ...)] (généré par NSwag) plante en 500 sur tout serveur
// dont la culture par défaut n'utilise pas le point comme séparateur décimal (ex. fr-FR → virgule) :
// RangeAttribute parse ses bornes avec CultureInfo.CurrentCulture, pas une culture invariante.
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);


// log file with serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(context.Configuration["AppLogs:FolderPath"] ?? "logs", "log-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"));

// Composition root du monolithe modulaire (DAT §4.2) : chaque module apporte ses contrôleurs (ApplicationPart) et handlers (scan MediatR), l'hôte n'assemble que ça.
builder.Services.AddControllers()
    .AddApplicationPart(typeof(PDOE.Dossiers.API.Controllers.DossiersController).Assembly)
    .AddApplicationPart(typeof(PDOE.Workflow.API.Controllers.WorkflowController).Assembly)
    .AddApplicationPart(typeof(PDOE.Execution.API.Controllers.ExecutionController).Assembly)
    .AddApplicationPart(typeof(PDOE.Apurement.API.Controllers.ApurementController).Assembly)
    .AddApplicationPart(typeof(PDOE.Reporting.API.Controllers.ReportingController).Assembly)
    .AddApplicationPart(typeof(PDOE.Admin.API.Controllers.ParametrageController).Assembly)
    .AddApplicationPart(typeof(PDOE.CBS.Integration.Controllers.TauxChangeController).Assembly)
    .AddApplicationPart(typeof(PDOE.Notifications.Controllers.NotificationsController).Assembly)
    .AddApplicationPart(typeof(PDOE.Gateway.Controllers.AuthController).Assembly);

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(PDOE.Dossiers.API.Controllers.DossiersController).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(PDOE.Workflow.API.Controllers.WorkflowController).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(PDOE.Execution.API.Controllers.ExecutionController).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(PDOE.Apurement.API.Controllers.ApurementController).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(PDOE.Reporting.API.Controllers.ReportingController).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(PDOE.Admin.API.Controllers.ParametrageController).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(PDOE.CBS.Integration.Controllers.TauxChangeController).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(PDOE.Notifications.Controllers.NotificationsController).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(PDOE.Gateway.Controllers.AuthController).Assembly);
});


// Le schéma PDOE_DB est possédé par le script DDL aux côtés de
// l'API (pas par des migrations EF Core) — cf. PdoeDbContext.
builder.Services.AddDbContext<PdoeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PdoeDb")));

// Stockage local pour dev/test, voir IFileStorageService.cs pour bascule vers IIS en prod.
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

// Notifications : journalise sans réseau en dev/test, voir INotificationSender.cs pour le plan de bascule HTTP.
builder.Services.AddSingleton<INotificationSender, LocalNotificationSender>();
builder.Services.AddHttpClient<INotificationSender, HttpNotificationSender>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Messagerie:BaseUrl"] ??
        throw new InvalidOperationException("Messagerie:BaseUrl is not configured."));
});

// Auth (PDOE.Gateway) : passerelle HTTP interne AFBCI vers l'AD (pas un bind LDAP direct, cf. HttpLdapAuthenticator —
// même principe que HttpNotificationSender), OTP en mémoire (jamais persisté, cf. IOtpChallengeStore), JWT émis par
// JwtTokenGenerator et validé ci-dessous.
builder.Services.AddHttpContextAccessor();
/*
builder.Services.AddHttpClient<ILdapAuthenticator, HttpLdapAuthenticator>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Ldap:BaseUrl"] ??
        throw new InvalidOperationException("Ldap:BaseUrl is not configured."));
});*/



var bypassLdap = builder.Configuration.GetValue<bool>("Ldap:BypassValidation");

if (builder.Environment.IsDevelopment() && bypassLdap)
{
    // Dev-only singleton bypass: no HTTP client or AD connection required
    builder.Services.AddSingleton<ILdapAuthenticator, BypassLdapAuthenticator>();
}
else
{
    // Real AD authentication setup
    builder.Services.AddHttpClient<ILdapAuthenticator, HttpLdapAuthenticator>(client =>
    {
        var baseUrl = builder.Configuration["Ldap:BaseUrl"]
            ?? throw new InvalidOperationException("Ldap:BaseUrl is missing from configuration.");

        client.BaseAddress = new Uri(baseUrl);
        // Sans ça, HttpClient retombe sur son timeout par défaut de 100s — l'utilisateur reste bloqué sur
        // "Connexion en cours..." bien trop longtemps si la passerelle AD interne est lente/indisponible.
        client.Timeout = TimeSpan.FromSeconds(15);
    });
}

// CBS (PDOE.CBS.Integration) : accès ABS2000 (taux de change, solde, signature) — même principe de bascule que Ldap ci-dessus.
var bypassCbs = builder.Configuration.GetValue<bool>("Cbs:BypassValidation");

if (builder.Environment.IsDevelopment() && bypassCbs)
{
    builder.Services.AddSingleton<ICbsClient, MockCbsClient>();
}
else
{
    builder.Services.AddHttpClient<ICbsClient, HttpCbsClient>(client =>
    {
        var baseUrl = builder.Configuration["Cbs:BaseUrl"]
            ?? throw new InvalidOperationException("Cbs:BaseUrl is missing from configuration.");

        client.BaseAddress = new Uri(baseUrl);
    });
}

// Application d'archivage externe (scénario hybride, cf. mémoire projet) : on pousse un signal "dossier archivé",
// l'appli externe vient chercher le détail via notre API (GET /dossiers?statut=ARCHIVE). Pas de bascule dev/prod
// comme Ldap/Cbs ci-dessus : NullArchiveNotifier ne bloque jamais l'archivage, que BaseUrl soit fournie ou non.
var archiveBaseUrl = builder.Configuration["ArchiveApp:BaseUrl"];
if (string.IsNullOrWhiteSpace(archiveBaseUrl))
{
    builder.Services.AddSingleton<IArchiveNotifier, NullArchiveNotifier>();
}
else
{
    builder.Services.AddHttpClient<IArchiveNotifier, HttpArchiveNotifier>(client =>
    {
        client.BaseAddress = new Uri(archiveBaseUrl);
        var apiKey = builder.Configuration["ArchiveApp:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }
    });
}

builder.Services.AddScoped<IOtpChallengeStore, DbOtpChallengeStore>();
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });


builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.AddPolicy("AdminDsiri", p => p.RequireRole("ADMIN_DSIRI", "SUPER_ADMIN"));
    options.AddPolicy("SuperAdmin", p => p.RequireRole("SUPER_ADMIN"));
});

// Déclenchent les alertes J-14/J-8/J0 (AlertesApurement) et retentent les notifications en échec, (ModuleMarker.cs de PDOE.Notifications, DeclarerExecutionHandler).
builder.Services.AddHostedService<PDOE.Workflow.API.BackgroundJobs.AlerteApurementSchedulerService>();
builder.Services.AddHostedService<PDOE.Notifications.BackgroundJobs.NotificationRetryService>();

// Dev only : ng serve tourne sur un port différent de PDOE.Api, d'où ce CORS. En prod, même reverse proxy donc pas de souci.
const string DevCors = "DevCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCors, policy =>
        policy.WithOrigins("http://localhost:4200", "https://xbkk45fh-4200.uks1.devtunnels.ms")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Pont statique vers HttpContext.User pour CurrentUser.Login — cf. commentaire de classe dans CurrentUser.cs.
CurrentUser.Configure(app.Services.GetRequiredService<IHttpContextAccessor>());

// Contrôleurs déclarent des routes nues, en prod le proxy ajoute /api. UsePathBase reproduit ça ici sans proxy devant.
app.UsePathBase("/api");

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseCors(DevCors);
}

app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (DomainException ex)
    {
        context.Response.StatusCode = ex.StatusCode;
        await context.Response.WriteAsJsonAsync(new ErrorResponse { Code = ex.Code, Message = ex.Message });
    }
    catch (AuthException ex)
    {
        context.Response.StatusCode = ex.StatusCode;
        await context.Response.WriteAsJsonAsync(new AuthErrorResponse { Code = ex.Code, Message = ex.Message });
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
