using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;

namespace PDOE.Apurement.API.Common;

/// Lit WorkflowEtapes.Ordre/Actif à chaque appel au lieu d'une table figée, pour qu'un réordonnancement Admin DSIRI pilote vraiment le routage.
internal static class WorkflowEngine
{
    /// <summary>Les 7 étapes historiques, dans leur ordre canonique — sert à dériver le code courant depuis StatutDossier.</summary>
    private static readonly IReadOnlyList<string> OrdreNiveauxHistorique =
    [
        "ETAPE_1_INITIATION", "ETAPE_2_GESTIONNAIRE", "ETAPE_3_COMEX",
        "ETAPE_4_TRESORERIE", "ETAPE_5_EXECUTION", "ETAPE_6_APUREMENT", "ETAPE_7_ARCHIVAGE",
    ];

    /// <summary>StatutDossier → index (0..6) dans OrdreNiveauxHistorique — cf. workflow-stepper.component.ts STATUT_VERS_INDEX_ETAPE (source de vérité).</summary>
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

    /// <summary>Code historique → statut "d'entrée" quand le dossier atterrit sur cette étape.</summary>
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

    /// <summary>Code d'étape (historique ou personnalisé) actuellement occupé par ce dossier.</summary>
    public static string CodeEtapeCourante(Dossier dossier)
    {
        if (dossier.EtapeGeneriqueCode is not null) return dossier.EtapeGeneriqueCode;

        var statut = Enum.Parse<StatutDossier>(dossier.StatutElectronique);
        var index = StatutVersIndexEtape.GetValueOrDefault(statut, 0);
        return OrdreNiveauxHistorique[index];
    }

    /// <summary>Résout le statut "d'entrée" d'un code historique — false si ce code n'est pas l'un des 7 historiques.</summary>
    public static bool TryGetStatutEntree(string code, out StatutDossier statut) =>
        EtapeVersStatutEntree.TryGetValue(code, out statut);

    /// <summary>Charge les étapes actives, triées par Ordre — source de vérité de la position dans le circuit.</summary>
    public static Task<List<WorkflowEtape>> ChargerEtapesActives(PdoeDbContext db, CancellationToken cancellationToken) =>
        db.WorkflowEtapes.Where(e => e.Actif).OrderBy(e => e.Ordre).ToListAsync(cancellationToken);

    /// Avance vers la prochaine étape ACTIVE configurée (historique ou perso) — utilisé par Soumettre et Valider. Ne fait rien si fin de circuit.
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

    /// <summary>Positionne le dossier sur l'étape donnée (générique ou historique) — utilisé par l'avancement et par le rejet vers une étape ciblée.</summary>
    public static void AtterrirSur(Dossier dossier, WorkflowEtape etape)
    {
        // Priorité au code, pas à TypeEtape : ETAPE_1/7 sont seedées en GENERIQUE mais restent historiques.
        // Tester TypeEtape d'abord casserait un rejet vers ETAPE_1_INITIATION.
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
