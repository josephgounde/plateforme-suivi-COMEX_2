using System.Reflection;
using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;
using PdfSharpCore.Utils;

namespace PDOE.Shared.Kernel.Pdf;

/// En-tête/pied de page partagés pour tous les exports PDF, mise en page du CDC_PDOE_v5.docx. Palette alignée sur pdoe-frontend/src/styles.scss.
public sealed class PdoeDocumentPdf : IDisposable
{
    public static readonly XColor Rouge = XColor.FromArgb(0xE3, 0x06, 0x13);
    public static readonly XColor RougeClair = XColor.FromArgb(0xFB, 0xE4, 0xE6);
    public static readonly XColor Encre = XColor.FromArgb(0x1A, 0x1A, 0x1A);
    public static readonly XColor Gris = XColor.FromArgb(0x8B, 0x8B, 0x8A);
    public static readonly XColor GrisClair = XColor.FromArgb(0xC0, 0xBF, 0xBF);
    public static readonly XColor GrisTresClair = XColor.FromArgb(0xF5, 0xF6, 0xF8);

    private const double MargeGauche = 40;
    private const double MargeDroite = 40;
    private const double HauteurEnTete = 62;
    private const double HauteurPiedDePage = 34;
    private const double SeuilBasDePage = 780;

    private static readonly Lazy<XImage> LogoPartage = new(ChargerLogo);

    private readonly PdfDocument _document = new();
    private readonly string _titreDocument;

    static PdoeDocumentPdf()
    {
        GlobalFontSettings.FontResolver = new FontResolver();
    }

    public PdoeDocumentPdf(string titreDocument)
    {
        _titreDocument = titreDocument;
        NouvellePage();
    }

    public PdfDocument Document => _document;
    public XGraphics Gfx { get; private set; } = null!;
    public double Y { get; set; }
    public double LargeurPage => _document.Pages[^1].Width.Point;
    public double LargeurUtile => LargeurPage - MargeGauche - MargeDroite;
    public double MargeX => MargeGauche;

    public void SautDePageSiNecessaire(double margeRestanteMin = 40)
    {
        if (Y > SeuilBasDePage - margeRestanteMin)
            NouvellePage();
    }

    public void NouvellePage()
    {
        // Un seul XGraphics ouvert par page à la fois, sinon PdfSharpCore lève "already exists for this page".
        Gfx?.Dispose();

        var page = _document.AddPage();
        Gfx = XGraphics.FromPdfPage(page);
        DessinerEnTete();
        Y = HauteurEnTete + 20;
    }

    /// Pour les titres de section (centré sur la largeur utile).
    public void TexteCentre(string texte, XFont police, XColor couleur, double? y = null)
    {
        var position = y ?? Y;
        Gfx.DrawString(texte, police, new XSolidBrush(couleur),
            new XRect(MargeGauche, position, LargeurUtile, police.Height), XStringFormats.TopCenter);
    }

    /// <summary>Finalise le document : tamponne le pied de page (filet + service + "Page X / Y") sur chaque page, une fois le nombre total de pages connu.</summary>
    private bool _pageCouranteFermee;

    public byte[] Finaliser(string ligneService)
    {
        Gfx?.Dispose();
        _pageCouranteFermee = true;

        var policePied = new XFont("Arial", 8, XFontStyle.Regular);
        var total = _document.PageCount;
        var formatDroite = new XStringFormat { Alignment = XStringAlignment.Far, LineAlignment = XLineAlignment.Near };

        for (var i = 0; i < total; i++)
        {
            var page = _document.Pages[i];
            using var gfx = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
            var yFilet = page.Height.Point - HauteurPiedDePage;

            gfx.DrawLine(new XPen(GrisClair, 0.75), MargeGauche, yFilet, page.Width.Point - MargeDroite, yFilet);
            gfx.DrawString(ligneService, policePied, new XSolidBrush(Gris), new XPoint(MargeGauche, yFilet + 15));
            gfx.DrawString($"Page {i + 1} / {total}", policePied, new XSolidBrush(Gris),
                new XRect(MargeGauche, yFilet + 8, page.Width.Point - MargeGauche - MargeDroite, 14), formatDroite);
        }

        using var stream = new MemoryStream();
        _document.Save(stream, false);
        return stream.ToArray();
    }

    private void DessinerEnTete()
    {
        var logo = LogoPartage.Value;
        const double largeurLogo = 85.0;
        var hauteurLogo = largeurLogo * logo.PixelHeight / logo.PixelWidth;
        Gfx.DrawImage(logo, MargeGauche, 12, largeurLogo, hauteurLogo);

        var policeTitre = new XFont("Arial", 12, XFontStyle.Bold);
        var policeSousTitre = new XFont("Arial", 8, XFontStyle.Regular);
        var formatDroite = new XStringFormat { Alignment = XStringAlignment.Far };

        Gfx.DrawString(_titreDocument, policeTitre, new XSolidBrush(Rouge),
            new XRect(MargeGauche, 16, LargeurUtile, 18), formatDroite);
        Gfx.DrawString("Afriland First Bank CI — Confidentiel", policeSousTitre, new XSolidBrush(Gris),
            new XRect(MargeGauche, 36, LargeurUtile, 14), formatDroite);

        Gfx.DrawLine(new XPen(Rouge, 1.2), MargeGauche, HauteurEnTete, LargeurPage - MargeDroite, HauteurEnTete);
    }

    private static XImage ChargerLogo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var flux = assembly.GetManifestResourceStream("PDOE.Shared.Kernel.Assets.logo-afriland-first-bank.png")
            ?? throw new InvalidOperationException("Logo embarqué introuvable — vérifier EmbeddedResource dans PDOE.Shared.Kernel.csproj.");
        using var memoire = new MemoryStream();
        flux.CopyTo(memoire);
        var octets = memoire.ToArray();
        return XImage.FromStream(() => new MemoryStream(octets));
    }

    public void Dispose()
    {
        if (!_pageCouranteFermee)
            Gfx?.Dispose();
    }
}
