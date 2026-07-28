using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Shared.Kernel.Common;
using PDOE.Shared.Kernel.Pdf;

namespace PDOE.Dossiers.API.Features.GenererFicheDossier;

/// Export interne, une page, sur le gabarit partagé PdoeDocumentPdf (cf. CDC_PDOE_v5.docx).
public class GenererFicheDossierHandler(PdoeDbContext db) : IRequestHandler<GenererFicheDossierQuery, byte[]>
{
    public async Task<byte[]> Handle(GenererFicheDossierQuery query, CancellationToken cancellationToken)
    {
        var dossier = await db.Dossiers.FirstOrDefaultAsync(d => d.DossierId == query.DossierId, cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        var culture = CultureInfo.GetCultureInfo("fr-FR");

        var lignes = new List<(string Label, string Valeur)>
        {
            ("Client", dossier.NomClient),
            ("Matricule client", dossier.MatriculeClient),
            ("Numéro de compte", dossier.NumCompte),
            ("Type d'opération", dossier.TypeOperation),
            ("Nature de la transaction", dossier.NatureTransaction),
            ("Référence de domiciliation", dossier.ReferenceDomiciliation ?? "—"),
            ("Code statistique opérateur", dossier.CodeStatistiqueOperateur ?? "—"),
            ("NIF client", dossier.NifClient ?? "—"),
            ("Montant", $"{dossier.Montant.ToString("N0", culture)} {dossier.Devise}"),
            ("Bénéficiaire", dossier.NomBeneficiaire),
            ("Pays du bénéficiaire", dossier.PaysBeneficiaire),
            ("Motif", dossier.Motif),
            ("Statut électronique", dossier.StatutElectronique),
            ("Gestionnaire assigné", dossier.GestionnaireAssigneLogin ?? "—"),
        };

        if (dossier.DateEcheanceApurement is not null)
            lignes.Add(("Échéance apurement", dossier.DateEcheanceApurement.Value.ToString("dd/MM/yyyy")));
        if (dossier.ReferenceSWIFT is not null)
            lignes.Add(("Référence SWIFT", dossier.ReferenceSWIFT));
        if (dossier.NumeroAC is not null)
            lignes.Add(("N° Attestation/Autorisation de Change", dossier.NumeroAC));
        if (dossier.CodeTRF is not null)
            lignes.Add(("Code TRF", dossier.CodeTRF));
        if (dossier.CorrespondantDesigne is not null)
            lignes.Add(("Correspondant", dossier.CorrespondantDesigne));

        using var pdf = new PdoeDocumentPdf($"Fiche Dossier — {dossier.ReferenceInterne}");

        var policeTitre = new XFont("Arial", 17, XFontStyle.Bold);
        var policeBadge = new XFont("Arial", 11, XFontStyle.Bold);
        var policeLabel = new XFont("Arial", 10, XFontStyle.Bold);
        var policeValeur = new XFont("Arial", 10, XFontStyle.Regular);

        pdf.TexteCentre("Fiche dossier de synthèse", policeTitre, PdoeDocumentPdf.Encre);
        pdf.Y += 26;

        // Badge référence — pilule rouge clair centrée, cf. palette du logo.
        var largeurBadge = policeBadge.Size * dossier.ReferenceInterne.Length * 0.62 + 24;
        var xBadge = pdf.MargeX + (pdf.LargeurUtile - largeurBadge) / 2;
        pdf.Gfx.DrawRoundedRectangle(new XSolidBrush(PdoeDocumentPdf.RougeClair), xBadge, pdf.Y, largeurBadge, 22, 6, 6);
        pdf.Gfx.DrawString(dossier.ReferenceInterne, policeBadge, new XSolidBrush(PdoeDocumentPdf.Rouge),
            new XRect(xBadge, pdf.Y, largeurBadge, 22), XStringFormats.Center);
        pdf.Y += 40;

        // Carte centrée — grille libellé/valeur à fond alterné, cf. mise en
        // page "centrée et moderne" demandée pour ces exports.
        const double largeurCarte = 460;
        var xCarte = pdf.MargeX + (pdf.LargeurUtile - largeurCarte) / 2;
        const double hauteurLigne = 22;
        var yCarteDebut = pdf.Y;

        for (var i = 0; i < lignes.Count; i++)
        {
            var (label, valeur) = lignes[i];
            var yLigne = pdf.Y;

            if (i % 2 == 1)
                pdf.Gfx.DrawRectangle(new XSolidBrush(PdoeDocumentPdf.GrisTresClair), xCarte, yLigne, largeurCarte, hauteurLigne);

            pdf.Gfx.DrawString(label, policeLabel, new XSolidBrush(PdoeDocumentPdf.Encre),
                new XRect(xCarte + 14, yLigne, 190, hauteurLigne), XStringFormats.CenterLeft);
            pdf.Gfx.DrawString(valeur, policeValeur, new XSolidBrush(PdoeDocumentPdf.Encre),
                new XRect(xCarte + 210, yLigne, largeurCarte - 224, hauteurLigne), XStringFormats.CenterLeft);

            pdf.Y += hauteurLigne;
        }

        pdf.Gfx.DrawRectangle(new XPen(PdoeDocumentPdf.GrisClair, 0.75), xCarte, yCarteDebut, largeurCarte, pdf.Y - yCarteDebut);

        return pdf.Finaliser("Direction du réseau — Département des Opérations - Service COMEX");
    }
}
