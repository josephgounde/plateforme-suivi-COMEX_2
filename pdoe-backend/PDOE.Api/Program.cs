using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Notifications;
using PDOE.Infrastructure.Storage;
using PDOE.Shared.Kernel.Common;

var builder = WebApplication.CreateBuilder(args);

// Composition root du monolithe modulaire (DAT §4.2) : chaque module apporte ses contrôleurs (ApplicationPart) et handlers (scan MediatR), l'hôte n'assemble que ça.
builder.Services.AddControllers()
    .AddApplicationPart(typeof(PDOE.Dossiers.API.Controllers.DossiersController).Assembly)
    .AddApplicationPart(typeof(PDOE.Workflow.API.Controllers.WorkflowController).Assembly)
    .AddApplicationPart(typeof(PDOE.Execution.API.Controllers.ExecutionController).Assembly)
    .AddApplicationPart(typeof(PDOE.Apurement.API.Controllers.ApurementController).Assembly)
    .AddApplicationPart(typeof(PDOE.Reporting.API.Controllers.ReportingController).Assembly)
    .AddApplicationPart(typeof(PDOE.Admin.API.Controllers.ParametrageController).Assembly)
    .AddApplicationPart(typeof(PDOE.CBS.Integration.Controllers.TauxChangeController).Assembly)
    .AddApplicationPart(typeof(PDOE.Notifications.Controllers.NotificationsController).Assembly);

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
});


// Le schéma PDOE_DB est possédé par le script DDL aux côtés de
// l'API (pas par des migrations EF Core) — cf. PdoeDbContext.
builder.Services.AddDbContext<PdoeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PdoeDb")));

// Stockage local pour dev/test, voir IFileStorageService.cs pour le plan de bascule vers IIS en prod.
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

// Notifications : journalise sans réseau en dev/test, voir INotificationSender.cs pour le plan de bascule HTTP.
builder.Services.AddSingleton<INotificationSender, LocalNotificationSender>();
/*builder.Services.AddHttpClient<INotificationSender, HttpNotificationSender>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Messagerie:BaseUrl"] ??
        throw new InvalidOperationException("Messagerie:BaseUrl is not configured."));
});*/

// Déclenchent les alertes J-14/J-8/J0 (AlertesApurement) et retentent les notifications en échec — cf. leurs
// commentaires de classe pour ce que ça referme (ModuleMarker.cs de PDOE.Notifications, DeclarerExecutionHandler).
builder.Services.AddHostedService<PDOE.Workflow.API.BackgroundJobs.AlerteApurementSchedulerService>();
builder.Services.AddHostedService<PDOE.Notifications.BackgroundJobs.NotificationRetryService>();

// Dev only : ng serve tourne sur un port différent de PDOE.Api, d'où ce CORS. En prod, même reverse proxy donc pas de souci.
const string DevCors = "DevCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCors, policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

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
});

app.UseAuthorization();

app.MapControllers();

app.Run();
