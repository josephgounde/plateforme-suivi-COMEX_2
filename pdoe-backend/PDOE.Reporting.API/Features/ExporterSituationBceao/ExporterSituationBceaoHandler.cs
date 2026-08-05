using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Infrastructure.Storage;
using PDOE.Reporting.API.Common;
using PDOE.Reporting.API.Reglementaire;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Reporting.API.Features.ExporterSituationBceao;

/// Gabarit BCEAO (situation-bceao.xlsx), même classification que le Trésor mais colonnes différentes.
/// "Qualité de la résidence" et "Secteur d'activité" restent vides — non modélisés dans Dossier.
public class ExporterSituationBceaoHandler(PdoeDbContext db, IFileStorageService storage) : IRequestHandler<ExporterSituationBceaoQuery, byte[]>
{
    public async Task<byte[]> Handle(ExporterSituationBceaoQuery query, CancellationToken cancellationToken)
    {
        var debut = query.Request.DateDebut.UtcDateTime.Date;
        var finDate = query.Request.DateFin.UtcDateTime.Date;
        var fin = finDate.AddDays(1).AddTicks(-1);

        var dossiers = await db.Dossiers
            .Where(d => d.DateExecution != null && d.DateExecution >= debut && d.DateExecution <= fin)
            .OrderBy(d => d.DateExecution)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        using var classeur = GabaritReglementaire.Ouvrir("situation-bceao.xlsx");

        var recuHorsUemoa = classeur.Worksheet("TRFT RECUS H ZON UEMOA");
        var recuUemoa = classeur.Worksheet("VIRMT RECU HORS CI UEMOA");
        var emisHorsUemoa = classeur.Worksheet("TRFT EMIS HORS ZO UEMOA");
        var emisUemoa = classeur.Worksheet("VIRMT EMIS HORS CI Z UEMOA");

        var ligneRecuHorsUemoa = 6;
        var ligneRecuUemoa = 6;
        var ligneEmisHorsUemoa = 6;
        var ligneEmisUemoa = 6;

        foreach (var dossier in dossiers)
        {
            var reception = TransfertClassification.EstReception(dossier);
            var uemoa = TransfertClassification.EstUemoa(dossier.PaysBeneficiaire);

            var feuille = (reception, uemoa) switch
            {
                (true, false) => recuHorsUemoa,
                (true, true) => recuUemoa,
                (false, false) => emisHorsUemoa,
                (false, true) => emisUemoa,
            };
            var ligne = (reception, uemoa) switch
            {
                (true, false) => ligneRecuHorsUemoa++,
                (true, true) => ligneRecuUemoa++,
                (false, false) => ligneEmisHorsUemoa++,
                (false, true) => ligneEmisUemoa++,
            };

            EcrireLigne(feuille, ligne, dossier);
        }

        using var stream = new MemoryStream();
        classeur.SaveAs(stream);
        var octets = stream.ToArray();

        var archive = await ExportReglementaireArchiver.ArchiverAsync(
            storage, "SITUATION_BCEAO", DateOnly.FromDateTime(debut), DateOnly.FromDateTime(finDate), octets, cancellationToken);

        var export = new ExportReglementaire
        {
            Categorie = "REGLEMENTAIRE",
            TypeExport = "SITUATION_BCEAO",
            DateDebut = DateOnly.FromDateTime(debut),
            DateFin = DateOnly.FromDateTime(finDate),
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

    // Mappage identique pour les 4 feuilles, seule la feuille cible change selon le sens du dossier.
    private static void EcrireLigne(IXLWorksheet feuille, int ligne, Dossier dossier)
    {
        var contreValeurFcfa = dossier.Devise == "XOF" ? dossier.Montant : dossier.Montant * (dossier.TauxChange ?? 1);

        feuille.Cell(ligne, 2).Value = dossier.DateExecution!.Value.ToString("dd/MM/yyyy");
        feuille.Cell(ligne, 3).Value = dossier.NomClient;
        feuille.Cell(ligne, 6).Value = dossier.Devise;
        feuille.Cell(ligne, 7).Value = dossier.Montant;
        feuille.Cell(ligne, 8).Value = contreValeurFcfa;
        feuille.Cell(ligne, 9).Value = dossier.Motif;
        feuille.Cell(ligne, 10).Value = dossier.NomBeneficiaire;
        feuille.Cell(ligne, 11).Value = dossier.PaysBeneficiaire;
    }
}
