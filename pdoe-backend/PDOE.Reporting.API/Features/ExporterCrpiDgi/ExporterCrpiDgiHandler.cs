using ClosedXML.Excel;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Reporting.API.Common;
using PDOE.Reporting.API.Reglementaire;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Reporting.API.Features.ExporterCrpiDgi;

/// Gabarit DGI (crpi-dgi.xlsx) — feuilles émis/reçus seulement, la DGI ne distingue pas UEMOA.
/// "Comptes commerciaux" reste vide : PDOE n'a pas de registre de comptes, à compléter à la main.
public class ExporterCrpiDgiHandler(PdoeDbContext db, IConfiguration configuration) : IRequestHandler<ExporterCrpiDgiQuery, byte[]>
{
    private const string NomBanque = "AFRILAND FIRST BANK CI";

    public async Task<byte[]> Handle(ExporterCrpiDgiQuery query, CancellationToken cancellationToken)
    {
        var debut = query.Request.DateDebut.UtcDateTime.Date;
        var finDate = query.Request.DateFin.UtcDateTime.Date;
        var fin = finDate.AddDays(1).AddTicks(-1);

        var dossiers = await db.Dossiers
            .Where(d => d.DateExecution != null && d.DateExecution >= debut && d.DateExecution <= fin)
            .OrderBy(d => d.DateExecution)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        using var classeur = GabaritReglementaire.Ouvrir("crpi-dgi.xlsx");

        var emis = classeur.Worksheet("TRFT EMIS T1 2026");
        var recu = classeur.Worksheet("TRFT RECU T1 2026");

        var ligneEmis = 14;
        var ligneRecu = 16;

        foreach (var dossier in dossiers)
        {
            var contreValeurFcfa = dossier.Devise == "XOF" ? dossier.Montant : dossier.Montant * (dossier.TauxChange ?? 1);
            var date = dossier.DateExecution!.Value;

            if (!TransfertClassification.EstReception(dossier))
            {
                var l = ligneEmis++;
                emis.Cell(l, 1).Value = dossier.NifClient ?? "";
                emis.Cell(l, 2).Value = dossier.NomClient;
                emis.Cell(l, 4).Value = dossier.TelephoneClient ?? "";
                emis.Cell(l, 6).Value = NomBanque;
                emis.Cell(l, 7).Value = dossier.NumCompte;
                emis.Cell(l, 8).Value = dossier.CodeTRF ?? "";
                emis.Cell(l, 9).Value = contreValeurFcfa;
                emis.Cell(l, 10).Value = dossier.Devise;
                emis.Cell(l, 12).Value = dossier.Motif;
                emis.Cell(l, 13).Value = date.Year;
                emis.Cell(l, 14).Value = date.ToString("dd/MM/yyyy");
                emis.Cell(l, 15).Value = dossier.NomBeneficiaire;
                emis.Cell(l, 18).Value = dossier.BanqueCorrespondanteIndicative ?? "";
                emis.Cell(l, 21).Value = dossier.PaysBeneficiaire;
            }
            else
            {
                var l = ligneRecu++;
                recu.Cell(l, 1).Value = dossier.NifClient ?? "";
                recu.Cell(l, 2).Value = dossier.NomClient;
                recu.Cell(l, 4).Value = dossier.TelephoneClient ?? "";
                recu.Cell(l, 6).Value = NomBanque;
                recu.Cell(l, 7).Value = dossier.NumCompte;
                recu.Cell(l, 8).Value = dossier.CodeTRF ?? "";
                recu.Cell(l, 9).Value = contreValeurFcfa;
                recu.Cell(l, 10).Value = dossier.Devise;
                recu.Cell(l, 12).Value = dossier.Motif;
                recu.Cell(l, 13).Value = date.Year;
                recu.Cell(l, 14).Value = date.ToString("dd/MM/yyyy");
                recu.Cell(l, 15).Value = dossier.NomBeneficiaire;
                recu.Cell(l, 18).Value = dossier.BanqueCorrespondanteIndicative ?? "";
                recu.Cell(l, 21).Value = dossier.PaysBeneficiaire;
            }
        }

        using var stream = new MemoryStream();
        classeur.SaveAs(stream);
        var octets = stream.ToArray();

        var archive = await ExportReglementaireArchiver.ArchiverAsync(
            configuration, "CRPI_DGI", DateOnly.FromDateTime(debut), DateOnly.FromDateTime(finDate), octets, cancellationToken);

        var export = new ExportReglementaire
        {
            Categorie = "REGLEMENTAIRE",
            TypeExport = "CRPI_DGI",
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
