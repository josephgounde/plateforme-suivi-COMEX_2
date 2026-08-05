using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Drawing;
using PDOE.Api.Contracts;
using PDOE.Dossiers.API.Common;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Infrastructure.Storage;
using PDOE.Shared.Kernel.Common;
using PDOE.Shared.Kernel.Pdf;

namespace PDOE.Dossiers.API.Features.GenererFicheDossier;

/// Export interne, sur le gabarit partagé PdoeDocumentPdf (cf. CDC_PDOE_v5.docx) : informations
/// générales, règle d'apurement applicable, documents joints et paiements partiels.
public class GenererFicheDossierHandler(PdoeDbContext db, IFileStorageService storage) : IRequestHandler<GenererFicheDossierQuery, byte[]>
{
    public async Task<byte[]> Handle(GenererFicheDossierQuery query, CancellationToken cancellationToken)
    {
        var dossier = await db.Dossiers.FirstOrDefaultAsync(d => d.DossierId == query.DossierId, cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        var documents = await db.Documents
            .Where(doc => doc.DossierId == query.DossierId)
            .OrderBy(doc => doc.CreatedAt)
            .ToListAsync(cancellationToken);

        var paiements = await db.PaiementsPartiels
            .Where(p => p.DossierId == query.DossierId)
            .OrderBy(p => p.DatePaiement)
            .ToListAsync(cancellationToken);

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
        var policeSection = new XFont("Arial", 12, XFontStyle.Bold);
        var policeLabel = new XFont("Arial", 10, XFontStyle.Bold);
        var policeValeur = new XFont("Arial", 10, XFontStyle.Regular);
        var policeTexte = new XFont("Arial", 9.5, XFontStyle.Regular);
        var policeVide = new XFont("Arial", 9.5, XFontStyle.Italic);

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

        pdf.Y += 32;

        // ── Règle d'apurement applicable ──
        pdf.SautDePageSiNecessaire(80);
        pdf.Gfx.DrawString("Règle d'apurement applicable", policeSection, new XSolidBrush(PdoeDocumentPdf.Rouge), new XPoint(pdf.MargeX, pdf.Y + 12));
        pdf.Y += 22;

        if (ReglesApurement.Table.TryGetValue(dossier.TypeOperation, out var regle))
        {
            pdf.Gfx.DrawString(regle.DelaiLibelle, policeTexte, new XSolidBrush(PdoeDocumentPdf.Encre),
                new XRect(pdf.MargeX, pdf.Y, pdf.LargeurUtile, 30), XStringFormats.TopLeft);
            pdf.Y += 28;
            pdf.Gfx.DrawString(regle.ReferenceReglementaire, policeVide, new XSolidBrush(PdoeDocumentPdf.Gris),
                new XRect(pdf.MargeX, pdf.Y, pdf.LargeurUtile, 14), XStringFormats.TopLeft);
            pdf.Y += 20;
        }
        else
        {
            pdf.Gfx.DrawString("Aucune règle d'apurement référencée pour ce type d'opération.", policeVide, new XSolidBrush(PdoeDocumentPdf.Gris), new XPoint(pdf.MargeX, pdf.Y + 10));
            pdf.Y += 20;
        }

        pdf.Y += 12;

        // ── Documents ──
        pdf.SautDePageSiNecessaire(80);
        pdf.Gfx.DrawString("Documents", policeSection, new XSolidBrush(PdoeDocumentPdf.Rouge), new XPoint(pdf.MargeX, pdf.Y + 12));
        pdf.Y += 22;

        if (documents.Count == 0)
        {
            pdf.Gfx.DrawString("Aucun document joint.", policeVide, new XSolidBrush(PdoeDocumentPdf.Gris), new XPoint(pdf.MargeX, pdf.Y + 10));
            pdf.Y += 20;
        }
        else
        {
            foreach (var document in documents)
            {
                pdf.SautDePageSiNecessaire(40);
                var yLigne = pdf.Y;

                pdf.Gfx.DrawString(document.TypeDocument, policeLabel, new XSolidBrush(PdoeDocumentPdf.Encre),
                    new XRect(pdf.MargeX, yLigne, 170, 16), XStringFormats.TopLeft);
                pdf.Gfx.DrawString(document.NomFichier, policeTexte, new XSolidBrush(PdoeDocumentPdf.Encre),
                    new XRect(pdf.MargeX + 170, yLigne, pdf.LargeurUtile - 260, 16), XStringFormats.TopLeft);

                var etat = document.EstObligatoire ? "Obligatoire" : "Facultatif";
                var validite = document.EstValide ? "Validé" : "Non validé";
                pdf.Gfx.DrawString($"{etat} — {validite}", policeVide, new XSolidBrush(PdoeDocumentPdf.Gris),
                    new XRect(pdf.MargeX, yLigne + 14, pdf.LargeurUtile, 14), XStringFormats.TopLeft);

                pdf.Y += 32;
            }
        }

        pdf.Y += 12;

        // ── Paiements partiels ──
        pdf.SautDePageSiNecessaire(80);
        pdf.Gfx.DrawString("Paiements partiels", policeSection, new XSolidBrush(PdoeDocumentPdf.Rouge), new XPoint(pdf.MargeX, pdf.Y + 12));
        pdf.Y += 22;

        if (paiements.Count == 0)
        {
            pdf.Gfx.DrawString("Aucun paiement partiel enregistré.", policeVide, new XSolidBrush(PdoeDocumentPdf.Gris), new XPoint(pdf.MargeX, pdf.Y + 10));
            pdf.Y += 20;
        }
        else
        {
            foreach (var paiement in paiements)
            {
                pdf.SautDePageSiNecessaire(40);
                var yLigne = pdf.Y;

                pdf.Gfx.DrawString(
                    $"{paiement.MontantPaiement.ToString("N0", culture)} {paiement.Devise} — {paiement.DatePaiement:dd/MM/yyyy}",
                    policeLabel, new XSolidBrush(PdoeDocumentPdf.Encre), new XPoint(pdf.MargeX, yLigne + 12));
                pdf.Gfx.DrawString(
                    $"Réf. {paiement.ReferencePaiement} — Solde restant : {paiement.SoldeRestant.ToString("N0", culture)} {paiement.Devise}",
                    policeVide, new XSolidBrush(PdoeDocumentPdf.Gris), new XRect(pdf.MargeX, yLigne + 14, pdf.LargeurUtile, 14), XStringFormats.TopLeft);

                pdf.Y += 32;
            }
        }

        var octets = pdf.Finaliser("Direction du réseau — Département des Opérations - Service COMEX");

        var archive = await ExportArchiver.ArchiverAsync(storage, "FICHE_DOSSIER", dossier.ReferenceInterne, octets, cancellationToken);
        var aujourdhui = DateOnly.FromDateTime(DateTime.UtcNow);
        var export = new ExportReglementaire
        {
            Categorie = "OPERATIONNEL",
            TypeExport = "FICHE_DOSSIER",
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
