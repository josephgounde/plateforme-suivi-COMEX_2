using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Infrastructure.Storage;
using PDOE.Reporting.API.Common;
using PDOE.Reporting.API.Excel;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Reporting.API.Features.ExporterRapportActiviteMensuel;

/// "Volume par type" = dossiers ouverts (CreatedAt) dans le mois. "Délai moyen"/"taux de rejet" = étapes de
/// workflow (DateAction) survenues dans le mois, même si le dossier a été ouvert un mois précédent — sinon un
/// dossier ouvert fin de mois N et décidé début N+1 ne remonterait dans aucun des deux rapports.
public class ExporterRapportActiviteMensuelHandler(PdoeDbContext db, IFileStorageService storage) : IRequestHandler<ExporterRapportActiviteMensuelQuery, byte[]>
{
    public async Task<byte[]> Handle(ExporterRapportActiviteMensuelQuery request, CancellationToken cancellationToken)
    {
        var mois = request.Mois ?? DateTime.UtcNow.ToString("yyyy-MM");
        var premierJourMois = DateOnly.ParseExact(mois + "-01", "yyyy-MM-dd");
        var dernierJourMois = premierJourMois.AddMonths(1).AddDays(-1);
        var borneDebut = premierJourMois.ToDateTime(TimeOnly.MinValue);
        var borneFin = dernierJourMois.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var dossiersDuMois = await db.Dossiers
            .Where(d => d.CreatedAt >= borneDebut && d.CreatedAt < borneFin)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var volumeParType = dossiersDuMois
            .GroupBy(d => d.TypeOperation)
            .ToDictionary(g => g.Key, g => g.Count());

        // Dossiers ayant au moins une étape dans le mois — peut inclure des dossiers ouverts avant le mois.
        var dossierIdsAvecActiviteCeMois = await db.EtapesWorkflow
            .Where(e => e.DateAction >= borneDebut && e.DateAction < borneFin)
            .Select(e => e.DossierId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var dossiersAvecActivite = await db.Dossiers
            .Include(d => d.EtapesWorkflow)
            .Where(d => dossierIdsAvecActiviteCeMois.Contains(d.DossierId))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        double totalHeures = 0;
        var nbIntervalles = 0;
        var nbValidations = 0;
        var nbRejets = 0;

        foreach (var dossier in dossiersAvecActivite)
        {
            var etapes = dossier.EtapesWorkflow.OrderBy(e => e.DateAction).ToList();
            for (var i = 1; i < etapes.Count; i++)
            {
                if (etapes[i].DateAction < borneDebut || etapes[i].DateAction >= borneFin) continue;

                var delta = (etapes[i].DateAction - etapes[i - 1].DateAction).TotalHours;
                if (delta > 0)
                {
                    totalHeures += delta;
                    nbIntervalles++;
                }
            }

            foreach (var etape in etapes)
            {
                if (etape.DateAction < borneDebut || etape.DateAction >= borneFin) continue;
                if (etape.Action == nameof(ActionWorkflow.VALIDATION)) nbValidations++;
                if (etape.Action == nameof(ActionWorkflow.REJET)) nbRejets++;
            }
        }

        var delaiMoyenHeures = nbIntervalles > 0 ? totalHeures / nbIntervalles : 0;
        var decisions = nbValidations + nbRejets;
        var tauxRejet = decisions > 0 ? (double)nbRejets / decisions : 0;

        using var classeur = new XLWorkbook();

        var feuilleSynthese = classeur.AddWorksheet("Synthèse");
        var ligneEntetesSynthese = PdoeWorkbookStyle.EcrireEntete(feuilleSynthese, "Activité mensuelle COMEX — Synthèse", 2);
        feuilleSynthese.Cell(ligneEntetesSynthese, 1).Value = "Indicateur";
        feuilleSynthese.Cell(ligneEntetesSynthese, 2).Value = "Valeur";
        feuilleSynthese.Cell(ligneEntetesSynthese + 1, 1).Value = "Période";
        feuilleSynthese.Cell(ligneEntetesSynthese + 1, 2).Value = mois;
        feuilleSynthese.Cell(ligneEntetesSynthese + 2, 1).Value = "Total dossiers";
        feuilleSynthese.Cell(ligneEntetesSynthese + 2, 2).Value = dossiersDuMois.Count;
        feuilleSynthese.Cell(ligneEntetesSynthese + 3, 1).Value = "Délai moyen entre étapes (heures)";
        feuilleSynthese.Cell(ligneEntetesSynthese + 3, 2).Value = Math.Round(delaiMoyenHeures, 1);
        feuilleSynthese.Cell(ligneEntetesSynthese + 4, 1).Value = "Taux de rejet";
        feuilleSynthese.Cell(ligneEntetesSynthese + 4, 2).Value = $"{Math.Round(tauxRejet * 100, 1)} %";
        PdoeWorkbookStyle.StylerTableau(feuilleSynthese, ligneEntetesSynthese, ligneEntetesSynthese + 4, 2);

        var feuilleVolume = classeur.AddWorksheet("Volume par type");
        var ligneEntetesVolume = PdoeWorkbookStyle.EcrireEntete(feuilleVolume, "Activité mensuelle COMEX — Volume par type", 2);
        feuilleVolume.Cell(ligneEntetesVolume, 1).Value = "Type d'opération";
        feuilleVolume.Cell(ligneEntetesVolume, 2).Value = "Volume";
        var ligne = ligneEntetesVolume + 1;
        foreach (var (type, count) in volumeParType)
        {
            feuilleVolume.Cell(ligne, 1).Value = type;
            feuilleVolume.Cell(ligne, 2).Value = count;
            ligne++;
        }
        PdoeWorkbookStyle.StylerTableau(feuilleVolume, ligneEntetesVolume, ligne - 1, 2);

        using var stream = new MemoryStream();
        classeur.SaveAs(stream);
        var octets = stream.ToArray();

        var archive = await ExportReglementaireArchiver.ArchiverAsync(
            storage, "ACTIVITE_MENSUELLE", premierJourMois, dernierJourMois, octets, cancellationToken);

        var export = new ExportReglementaire
        {
            Categorie = "OPERATIONNEL",
            TypeExport = "ACTIVITE_MENSUELLE",
            DateDebut = premierJourMois,
            DateFin = dernierJourMois,
            NomFichier = archive.NomFichier,
            CheminFichier = archive.Chemin,
            HashSHA256 = archive.HashSHA256,
            TailleFichier = archive.Taille,
            CreatedBy = CurrentUser.Login,
        };
        db.ExportsReglementaires.Add(export);
        await db.SaveChangesAsync(cancellationToken);

        JournalAuditWriter.EnregistrerExport(db, export);
        await db.SaveChangesAsync(cancellationToken);

        return octets;
    }
}
