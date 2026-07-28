using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;

namespace PDOE.Execution.API.Common;

// Dupliqué de PDOE.Workflow.API.Common.WorkflowEngine (modules indépendants, pas de ProjectReference).
// Le routage suit WorkflowEtapes.Ordre/Actif, pas un statut EXECUTE figé — sinon le dossier reste bloqué.
internal static class WorkflowEngine
{
    private static readonly IReadOnlyList<string> OrdreNiveauxHistorique =
    [
        "ETAPE_1_INITIATION", "ETAPE_2_GESTIONNAIRE", "ETAPE_3_COMEX",
        "ETAPE_4_TRESORERIE", "ETAPE_5_EXECUTION", "ETAPE_6_APUREMENT", "ETAPE_7_ARCHIVAGE",
    ];

    private static readonly IReadOnlyDictionary<StatutDossier, int> StatutVersIndexEtape = new Dictionary<StatutDossier, int>
    {
        [StatutDossier.BROUILLON] = 0,
        [StatutDossier.INITIE] = 0,
        [StatutDossier.EN_VALIDATION_GESTIONNAIRE] = 1,
        [StatutDossier.CONFIRME_GESTIONNAIRE] = 1,
        [StatutDossier.EN_CONTROLE_COMEX] = 2,
        [StatutDossier.VALIDE_COMEX] = 2,
        [StatutDossier.EN_AVIS_TRESORERIE] = 3,
        [StatutDossier.AVIS_TRESORERIE_DONNE] = 3,
        [StatutDossier.ANTI_FRACTIONNEMENT_DETECTE] = 4,
        [StatutDossier.EN_ATTENTE_EXECUTION] = 4,
        [StatutDossier.EN_EXECUTION_SWIFT] = 4,
        [StatutDossier.EXECUTE] = 4,
        [StatutDossier.EN_APUREMENT] = 5,
        [StatutDossier.APUREMENT_PARTIEL] = 5,
        [StatutDossier.ALERTE_J14] = 5,
        [StatutDossier.ALERTE_J8] = 5,
        [StatutDossier.DEPASSE_BCEAO] = 5,
        [StatutDossier.APURE] = 5,
        [StatutDossier.EN_ARCHIVAGE] = 6,
        [StatutDossier.ARCHIVE] = 6,
        // REJETE_DEFINITIF : pas d'entrée — n'appartient à aucune étape normale.
    };

    private static readonly IReadOnlyDictionary<string, StatutDossier> EtapeVersStatutEntree = new Dictionary<string, StatutDossier>
    {
        ["ETAPE_1_INITIATION"] = StatutDossier.BROUILLON,
        ["ETAPE_2_GESTIONNAIRE"] = StatutDossier.EN_VALIDATION_GESTIONNAIRE,
        ["ETAPE_3_COMEX"] = StatutDossier.EN_CONTROLE_COMEX,
        ["ETAPE_4_TRESORERIE"] = StatutDossier.EN_AVIS_TRESORERIE,
        ["ETAPE_5_EXECUTION"] = StatutDossier.EN_ATTENTE_EXECUTION,
        ["ETAPE_6_APUREMENT"] = StatutDossier.EN_APUREMENT,
        ["ETAPE_7_ARCHIVAGE"] = StatutDossier.EN_ARCHIVAGE,
    };

    public static string CodeEtapeCourante(Dossier dossier)
    {
        if (dossier.EtapeGeneriqueCode is not null) return dossier.EtapeGeneriqueCode;

        var statut = Enum.Parse<StatutDossier>(dossier.StatutElectronique);
        var index = StatutVersIndexEtape.GetValueOrDefault(statut, 0);
        return OrdreNiveauxHistorique[index];
    }

    public static bool TryGetStatutEntree(string code, out StatutDossier statut) =>
        EtapeVersStatutEntree.TryGetValue(code, out statut);

    public static Task<List<WorkflowEtape>> ChargerEtapesActives(PdoeDbContext db, CancellationToken cancellationToken) =>
        db.WorkflowEtapes.Where(e => e.Actif).OrderBy(e => e.Ordre).ToListAsync(cancellationToken);

    public static async Task AvancerVersEtapeSuivante(PdoeDbContext db, Dossier dossier, CancellationToken cancellationToken)
    {
        var actives = await ChargerEtapesActives(db, cancellationToken);

        var codeCourant = CodeEtapeCourante(dossier);
        var idx = actives.FindIndex(e => e.Code == codeCourant);
        var suivante = idx >= 0 && idx + 1 < actives.Count ? actives[idx + 1] : null;

        dossier.EtapeGeneriqueCode = null;
        dossier.SousEtatGenerique = null;

        if (suivante is null) return;

        AtterrirSur(dossier, suivante);
    }

    public static void AtterrirSur(Dossier dossier, WorkflowEtape etape)
    {
        // ETAPE_1_INITIATION/7_ARCHIVAGE sont en TypeEtape=GENERIQUE mais restent des codes historiques.
        // EtapeGeneriqueCode ne sert que pour les étapes hors de ces 7.
        if (TryGetStatutEntree(etape.Code, out var statutCible))
        {
            dossier.EtapeGeneriqueCode = null;
            dossier.SousEtatGenerique = null;
            dossier.StatutElectronique = statutCible.ToString();
        }
        else
        {
            dossier.EtapeGeneriqueCode = etape.Code;
            dossier.SousEtatGenerique = nameof(SousEtat.EN_ATTENTE);
        }
    }
}
