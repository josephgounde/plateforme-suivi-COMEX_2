using ClosedXML.Excel;
using MediatR;
using Microsoft.Extensions.Configuration;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Reporting.API.Common;
using PDOE.Reporting.API.Excel;
using PDOE.Reporting.API.Features.GetDossiersEnRetard;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Reporting.API.Features.ExporterDossiersEnRetard;

/// Même données que GET /reporting/dossiers-en-retard, en xlsx — cf. mockExporterDossiersEnRetard côté frontend.
public class ExporterDossiersEnRetardHandler(IMediator mediator, PdoeDbContext db, IConfiguration configuration) : IRequestHandler<ExporterDossiersEnRetardQuery, byte[]>
{
    public async Task<byte[]> Handle(ExporterDossiersEnRetardQuery request, CancellationToken cancellationToken)
    {
        var items = await mediator.Send(new GetDossiersEnRetardQuery(), cancellationToken);

        using var classeur = new XLWorkbook();
        var feuille = classeur.AddWorksheet("Dossiers en retard");

        string[] entetes =
        [
            "Référence", "Client", "Étape bloquée", "Heures depuis dernière action",
            "Seuil dépassé (%)", "Dernier agent",
        ];

        var ligneEntetes = PdoeWorkbookStyle.EcrireEntete(feuille, "Dossiers en retard", entetes.Length);
        for (var i = 0; i < entetes.Length; i++)
            feuille.Cell(ligneEntetes, i + 1).Value = entetes[i];

        var ligne = ligneEntetes + 1;
        foreach (var item in items)
        {
            feuille.Cell(ligne, 1).Value = item.ReferenceInterne;
            feuille.Cell(ligne, 2).Value = item.NomClient;
            feuille.Cell(ligne, 3).Value = item.DerniereEtape;
            feuille.Cell(ligne, 4).Value = item.HeuresDepuisDerniereAction;
            feuille.Cell(ligne, 5).Value = item.SeuilDepasse;
            feuille.Cell(ligne, 6).Value = item.DernierAgent;
            ligne++;
        }

        PdoeWorkbookStyle.StylerTableau(feuille, ligneEntetes, ligne - 1, entetes.Length);

        using var stream = new MemoryStream();
        classeur.SaveAs(stream);
        var octets = stream.ToArray();

        // Rapport instantané (pas de période) : DateDebut = DateFin = date du jour.
        var aujourdhui = DateOnly.FromDateTime(DateTime.UtcNow);
        var archive = await ExportReglementaireArchiver.ArchiverAsync(
            configuration, "DOSSIERS_EN_RETARD", aujourdhui, aujourdhui, octets, cancellationToken);

        var export = new ExportReglementaire
        {
            Categorie = "OPERATIONNEL",
            TypeExport = "DOSSIERS_EN_RETARD",
            DateDebut = aujourdhui,
            DateFin = aujourdhui,
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
