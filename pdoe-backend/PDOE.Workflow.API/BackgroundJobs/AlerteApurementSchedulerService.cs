using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Infrastructure.Notifications;
using PDOE.Workflow.API.Common;

namespace PDOE.Workflow.API.BackgroundJobs;

/// Déclenche les notifications J-14/J-8/J0 planifiées par DeclarerExecutionHandler (table AlertesApurement) et
/// fait progresser StatutDossier vers ALERTE_J14/ALERTE_J8 en conséquence. Sans ce job, les alertes restent en
/// base sans jamais être envoyées, et ALERTE_J14/ALERTE_J8 ne sont jamais atteints — cf. GetDashboardHandler et
/// GetDossiersEnRetardHandler, qui comptent ALERTE_J8 comme statut urgent.
public class AlerteApurementSchedulerService(IServiceScopeFactory scopeFactory, ILogger<AlerteApurementSchedulerService> logger) : BackgroundService
{
    private static readonly TimeSpan Intervalle = TimeSpan.FromMinutes(15);

    // "comex" : compte de service existant (cf. SoumettreDossierHandler/DeclarerDepassementHandler), pas de
    // compte "SYSTEM" dans Utilisateurs — EtapesWorkflow.AgentLogin a une vraie FK vers Utilisateurs.LoginAD.
    private const string AgentSysteme = "comex";

    private static readonly HashSet<string> StatutsEligiblesJ14 =
    [
        nameof(StatutDossier.EXECUTE),
        nameof(StatutDossier.EN_APUREMENT),
        nameof(StatutDossier.APUREMENT_PARTIEL),
    ];

    private static readonly HashSet<string> StatutsEligiblesJ8 =
    [
        nameof(StatutDossier.EXECUTE),
        nameof(StatutDossier.EN_APUREMENT),
        nameof(StatutDossier.APUREMENT_PARTIEL),
        nameof(StatutDossier.ALERTE_J14),
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Intervalle);
        do
        {
            try
            {
                await TraiterAlertesEnAttente(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Échec du traitement des alertes d'apurement.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task TraiterAlertesEnAttente(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PdoeDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<INotificationSender>();

        var now = DateTime.UtcNow;

        // Tri par DateAlerte : garantit J14 avant J8 avant J0 pour un même dossier dans un même passage
        // (sinon un dossier avec plusieurs alertes en retard pourrait sauter directement à ALERTE_J8).
        var alertes = await db.AlertesApurement
            .Include(a => a.Dossier)
            .Where(a => !a.Envoye && a.DateAlerte <= now && !a.Dossier.ApurementComplet)
            .OrderBy(a => a.DateAlerte)
            .ToListAsync(cancellationToken);

        foreach (var alerte in alertes)
        {
            var dossier = alerte.Dossier;
            string? destinataireLogin = null;

            switch (alerte.TypeAlerte)
            {
                case nameof(TypeAlerte.RELANCE_J14):
                    if (StatutsEligiblesJ14.Contains(dossier.StatutElectronique))
                    {
                        FaireProgresser(db, dossier, nameof(StatutDossier.ALERTE_J14), "APUREMENT_ALERTE_J14", now);
                        destinataireLogin = dossier.GestionnaireAssigneLogin ?? AgentSysteme;
                    }
                    break;

                case nameof(TypeAlerte.MISE_EN_DEMEURE_J8):
                    if (StatutsEligiblesJ8.Contains(dossier.StatutElectronique))
                    {
                        FaireProgresser(db, dossier, nameof(StatutDossier.ALERTE_J8), "APUREMENT_ALERTE_J8", now);
                        destinataireLogin = AgentSysteme;
                    }
                    break;

                case nameof(TypeAlerte.DEPASSEMENT_J0):
                    // Pas de transition automatique vers DEPASSE_BCEAO : ça reste l'action humaine attendue
                    // (cf. DeclarerDepassementHandler). On se contente de relancer.
                    if (StatutsEligiblesJ8.Contains(dossier.StatutElectronique) || dossier.StatutElectronique == nameof(StatutDossier.ALERTE_J8))
                        destinataireLogin = AgentSysteme;
                    break;
            }

            if (destinataireLogin is not null)
            {
                await NotificationWriter.EnregistrerEtEnvoyer(
                    db, sender, dossier.DossierId, alerte.TypeAlerte, $"{destinataireLogin}@afbci.ci", cancellationToken);
            }

            alerte.Envoye = true;
            alerte.DateEnvoi = now;
        }

        if (alertes.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("{Count} alerte(s) d'apurement traitée(s).", alertes.Count);
        }
    }

    private static void FaireProgresser(PdoeDbContext db, Dossier dossier, string nouveauStatut, string niveauValidation, DateTime now)
    {
        if (dossier.StatutElectronique == nouveauStatut) return;

        var statutAvant = dossier.StatutElectronique;
        dossier.StatutElectronique = nouveauStatut;
        dossier.UpdatedAt = now;
        dossier.UpdatedBy = AgentSysteme;

        db.EtapesWorkflow.Add(new EtapeWorkflow
        {
            DossierId = dossier.DossierId,
            NiveauValidation = niveauValidation,
            StatutAvant = statutAvant,
            StatutApres = nouveauStatut,
            Action = nameof(ActionWorkflow.ESCALADE),
            AgentLogin = AgentSysteme,
            DateAction = now,
            CreatedAt = now,
            CreatedBy = "SYSTEM",
        });
    }
}
