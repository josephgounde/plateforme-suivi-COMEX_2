using Microsoft.EntityFrameworkCore;
using PDOE.Infrastructure.Entities;

namespace PDOE.Infrastructure;

/// Schéma issu du script DDL (13 tables.sql), pas de migrations EF Core. Pas besoin de Database.Migrate()/EnsureCreated() hors tests.
public class PdoeDbContext(DbContextOptions<PdoeDbContext> options) : DbContext(options)
{
    public DbSet<ParametreMetier> ParametresMetier => Set<ParametreMetier>();
    public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();
    public DbSet<Dossier> Dossiers => Set<Dossier>();
    public DbSet<GestionnaireClient> GestionnaireClients => Set<GestionnaireClient>();
    public DbSet<EtapeWorkflow> EtapesWorkflow => Set<EtapeWorkflow>();
    public DbSet<WorkflowEtape> WorkflowEtapes => Set<WorkflowEtape>();
    public DbSet<ChecklistItemConfig> ChecklistItemsConfig => Set<ChecklistItemConfig>();
    public DbSet<PaiementPartiel> PaiementsPartiels => Set<PaiementPartiel>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AlerteApurement> AlertesApurement => Set<AlerteApurement>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
    public DbSet<JournalAudit> JournalAudit => Set<JournalAudit>();
    public DbSet<ExportReglementaire> ExportsReglementaires => Set<ExportReglementaire>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PdoeDbContext).Assembly);
    }
}
