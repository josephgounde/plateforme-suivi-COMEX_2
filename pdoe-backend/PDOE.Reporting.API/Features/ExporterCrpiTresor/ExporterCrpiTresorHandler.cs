using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Infrastructure.Storage;
using PDOE.Reporting.API.Common;
using PDOE.Reporting.API.Reglementaire;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Reporting.API.Features.ExporterCrpiTresor;

/// Gabarit Trésor (crpi-tresor.xlsx) — 4 feuilles reçu/émis × UEMOA/hors UEMOA.
/// En-têtes de période pas réécrites (gabarit source incohérent, à vérifier à la main) ; NUMERO AC seulement sur "EMIS HORS UEMOA".
public class ExporterCrpiTresorHandler(PdoeDbContext db, IFileStorageService storage) : IRequestHandler<ExporterCrpiTresorQuery, byte[]>
{
    public async Task<byte[]> Handle(ExporterCrpiTresorQuery query, CancellationToken cancellationToken)
    {
        var debut = query.Request.DateDebut.UtcDateTime.Date;
        var finDate = query.Request.DateFin.UtcDateTime.Date;
        var fin = finDate.AddDays(1).AddTicks(-1);

        var dossiers = await db.Dossiers
            .Where(d => d.DateExecution != null && d.DateExecution >= debut && d.DateExecution <= fin)
            .OrderBy(d => d.DateExecution)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        using var classeur = GabaritReglementaire.Ouvrir("crpi-tresor.xlsx");

        var recuHorsUemoa = classeur.Worksheet("TRF RECU HORS UEMOA FEVR 2026");
        var emisHorsUemoa = classeur.Worksheet("TRFT EMIS HORS UEMOA FEVR 2026");
        var emisUemoa = classeur.Worksheet("VIRMT EMIS UEMOA FEVR 2026");
        var recuUemoa = classeur.Worksheet("VIRMT RECU UEMOA JANV 2026");

        var ligneRecuHorsUemoa = 13;
        var ligneEmisHorsUemoa = 13;
        var ligneEmisUemoa = 13;
        var ligneRecuUemoa = 13;

        foreach (var dossier in dossiers)
        {
            var reception = TransfertClassification.EstReception(dossier);
            var uemoa = TransfertClassification.EstUemoa(dossier.PaysBeneficiaire);
            var contreValeurFcfa = dossier.Devise == "XOF" ? dossier.Montant : dossier.Montant * (dossier.TauxChange ?? 1);
            var dateOperation = dossier.DateExecution!.Value.ToString("dd/MM/yyyy");

            if (reception && !uemoa)
            {
                var l = ligneRecuHorsUemoa++;
                recuHorsUemoa.Cell(l, 1).Value = dateOperation;
                recuHorsUemoa.Cell(l, 2).Value = dossier.NomBeneficiaire;
                recuHorsUemoa.Cell(l, 3).Value = dossier.BanqueCorrespondanteIndicative ?? "";
                recuHorsUemoa.Cell(l, 4).Value = dossier.NomClient;
                recuHorsUemoa.Cell(l, 5).Value = dossier.Devise;
                recuHorsUemoa.Cell(l, 6).Value = dossier.Montant;
                recuHorsUemoa.Cell(l, 7).Value = contreValeurFcfa;
                recuHorsUemoa.Cell(l, 8).Value = dossier.PaysBeneficiaire;
                recuHorsUemoa.Cell(l, 9).Value = dossier.Motif;
                recuHorsUemoa.Cell(l, 10).Value = dossier.CodeStatistiqueOperateur ?? "";
            }
            else if (!reception && !uemoa)
            {
                var l = ligneEmisHorsUemoa++;
                emisHorsUemoa.Cell(l, 2).Value = dateOperation;
                emisHorsUemoa.Cell(l, 3).Value = dossier.NumeroAC ?? "";
                emisHorsUemoa.Cell(l, 4).Value = dossier.NomClient;
                emisHorsUemoa.Cell(l, 5).Value = dossier.NomBeneficiaire;
                emisHorsUemoa.Cell(l, 6).Value = dossier.BanqueCorrespondanteIndicative ?? "";
                emisHorsUemoa.Cell(l, 7).Value = dossier.Devise;
                emisHorsUemoa.Cell(l, 8).Value = dossier.Montant;
                emisHorsUemoa.Cell(l, 9).Value = contreValeurFcfa;
                emisHorsUemoa.Cell(l, 10).Value = dossier.PaysBeneficiaire;
                emisHorsUemoa.Cell(l, 11).Value = dossier.Motif;
                emisHorsUemoa.Cell(l, 12).Value = dossier.CodeStatistiqueOperateur ?? "";
            }
            else if (!reception)
            {
                var l = ligneEmisUemoa++;
                emisUemoa.Cell(l, 1).Value = dateOperation;
                emisUemoa.Cell(l, 2).Value = dossier.NomClient;
                emisUemoa.Cell(l, 3).Value = dossier.NomBeneficiaire;
                emisUemoa.Cell(l, 4).Value = dossier.BanqueCorrespondanteIndicative ?? "";
                emisUemoa.Cell(l, 5).Value = dossier.Montant;
                emisUemoa.Cell(l, 6).Value = dossier.PaysBeneficiaire;
                emisUemoa.Cell(l, 7).Value = dossier.Motif;
                emisUemoa.Cell(l, 8).Value = dossier.CodeStatistiqueOperateur ?? "";
            }
            else
            {
                var l = ligneRecuUemoa++;
                recuUemoa.Cell(l, 1).Value = dateOperation;
                recuUemoa.Cell(l, 2).Value = dossier.NomBeneficiaire;
                recuUemoa.Cell(l, 3).Value = dossier.BanqueCorrespondanteIndicative ?? "";
                recuUemoa.Cell(l, 4).Value = dossier.NomClient;
                recuUemoa.Cell(l, 5).Value = dossier.Montant;
                recuUemoa.Cell(l, 6).Value = dossier.PaysBeneficiaire;
                recuUemoa.Cell(l, 7).Value = dossier.Motif;
                recuUemoa.Cell(l, 8).Value = dossier.CodeStatistiqueOperateur ?? "";
            }
        }

        using var stream = new MemoryStream();
        classeur.SaveAs(stream);
        var octets = stream.ToArray();

        var archive = await ExportReglementaireArchiver.ArchiverAsync(
            storage, "CRPI_TRESOR", DateOnly.FromDateTime(debut), DateOnly.FromDateTime(finDate), octets, cancellationToken);

        var export = new ExportReglementaire
        {
            Categorie = "REGLEMENTAIRE",
            TypeExport = "CRPI_TRESOR",
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
}
