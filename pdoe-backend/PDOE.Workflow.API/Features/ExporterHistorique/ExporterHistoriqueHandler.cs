using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Infrastructure.Storage;
using PDOE.Shared.Kernel.Common;
using PDOE.Shared.Kernel.Pdf;
using PDOE.Workflow.API.Common;

namespace PDOE.Workflow.API.Features.ExporterHistorique;

/// Export interne (Agent COMEX, Audit) — mêmes données que GET /workflow/{dossierId}/historique, mis en forme en PDF via PdoeDocumentPdf.
public class ExporterHistoriqueHandler(PdoeDbContext db, IFileStorageService storage) : IRequestHandler<ExporterHistoriqueQuery, byte[]>
{
    public async Task<byte[]> Handle(ExporterHistoriqueQuery query, CancellationToken cancellationToken)
    {
        var dossier = await db.Dossiers.FirstOrDefaultAsync(d => d.DossierId == query.DossierId, cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        var etapes = await db.EtapesWorkflow
            .Where(e => e.DossierId == query.DossierId)
            .OrderBy(e => e.DateAction)
            .ToListAsync(cancellationToken);

        var culture = CultureInfo.GetCultureInfo("fr-FR");

        using var pdf = new PdoeDocumentPdf($"Historique du Circuit — {dossier.ReferenceInterne}");

        var policeTitre = new XFont("Arial", 17, XFontStyle.Bold);
        var policeClient = new XFont("Arial", 10, XFontStyle.Regular);
        var policeAction = new XFont("Arial", 10.5, XFontStyle.Bold);
        var policeDate = new XFont("Arial", 9, XFontStyle.Regular);
        var policeDetail = new XFont("Arial", 9, XFontStyle.Regular);

        pdf.TexteCentre("Historique du circuit de validation", policeTitre, PdoeDocumentPdf.Encre);
        pdf.Y += 20;
        pdf.TexteCentre(dossier.NomClient, policeClient, PdoeDocumentPdf.Gris);
        pdf.Y += 28;

        const double largeurCarte = 480;
        var xCarte = pdf.MargeX + (pdf.LargeurUtile - largeurCarte) / 2;
        const double bandeAccent = 3;

        foreach (var etape in etapes)
        {
            var hauteurCarte = 20 + 15 + (etape.MotifRejet is not null ? 15 : 0) + 10;

            pdf.SautDePageSiNecessaire(hauteurCarte + 20);

            var yCarte = pdf.Y;
            pdf.Gfx.DrawRectangle(new XSolidBrush(PdoeDocumentPdf.GrisTresClair), xCarte, yCarte, largeurCarte, hauteurCarte);
            pdf.Gfx.DrawRectangle(new XSolidBrush(PdoeDocumentPdf.Rouge), xCarte, yCarte, bandeAccent, hauteurCarte);

            var yTexte = yCarte + 6;
            pdf.Gfx.DrawString(etape.Action, policeAction, new XSolidBrush(PdoeDocumentPdf.Rouge), new XPoint(xCarte + 14, yTexte + 9));
            pdf.Gfx.DrawString(
                etape.DateAction.ToString("dd/MM/yyyy HH:mm:ss", culture),
                policeDate, new XSolidBrush(PdoeDocumentPdf.Gris),
                new XRect(xCarte, yTexte, largeurCarte - 14, 12), XStringFormats.TopRight);
            yTexte += 18;

            pdf.Gfx.DrawString($"Agent : {etape.AgentLogin}", policeDetail, new XSolidBrush(PdoeDocumentPdf.Encre), new XPoint(xCarte + 14, yTexte + 8));
            yTexte += 15;

            if (etape.MotifRejet is not null)
            {
                pdf.Gfx.DrawString($"Motif : {etape.MotifRejet}", policeDetail, new XSolidBrush(PdoeDocumentPdf.Encre), new XPoint(xCarte + 14, yTexte + 8));
            }

            pdf.Y += hauteurCarte + 10;
        }

        var octets = pdf.Finaliser("Direction des Opérations Internationales — Service COMEX");

        var archive = await ExportArchiver.ArchiverAsync(storage, "HISTORIQUE_DOSSIER", dossier.ReferenceInterne, octets, cancellationToken);
        var aujourdhui = DateOnly.FromDateTime(DateTime.UtcNow);
        var export = new ExportReglementaire
        {
            Categorie = "OPERATIONNEL",
            TypeExport = "HISTORIQUE_DOSSIER",
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

        JournalAuditWriter.EnregistrerExport(db, export, dossier.ReferenceInterne);
        await db.SaveChangesAsync(cancellationToken);

        return octets;
    }
}
