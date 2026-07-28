using ClosedXML.Excel;

namespace PDOE.Reporting.API.Excel;

/// Palette alignée sur le logo Afriland (même palette que le PDF, cf. PdoeDocumentPdf).
public static class PdoeWorkbookStyle
{
    public static readonly XLColor Rouge = XLColor.FromHtml("#E30613");
    public static readonly XLColor Encre = XLColor.FromHtml("#1A1A1A");
    public static readonly XLColor Gris = XLColor.FromHtml("#8B8B8A");
    public static readonly XLColor GrisTresClair = XLColor.FromHtml("#F5F6F8");
    public static readonly XLColor Blanc = XLColor.White;

    /// Retourne le numéro de la première ligne libre après le bandeau (toujours 4).
    public static int EcrireEntete(IXLWorksheet feuille, string titre, int nbColonnes)
    {
        var titreCellule = feuille.Range(1, 1, 1, nbColonnes).Merge().FirstCell();
        titreCellule.Value = "Afriland First Bank CI — " + titre;
        titreCellule.Style.Font.Bold = true;
        titreCellule.Style.Font.FontSize = 14;
        titreCellule.Style.Font.FontColor = Rouge;
        titreCellule.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        feuille.Row(1).Height = 22;

        var sousTitreCellule = feuille.Range(2, 1, 2, nbColonnes).Merge().FirstCell();
        sousTitreCellule.Value = $"Rapport généré le {DateTime.UtcNow:dd/MM/yyyy à HH:mm}";
        sousTitreCellule.Style.Font.FontColor = Gris;
        sousTitreCellule.Style.Font.FontSize = 9;
        sousTitreCellule.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        feuille.Row(2).Height = 16;

        return 4;
    }

    public static void StylerTableau(IXLWorksheet feuille, int ligneEntetes, int ligneFinDonnees, int nbColonnes)
    {
        var entetes = feuille.Range(ligneEntetes, 1, ligneEntetes, nbColonnes);
        entetes.Style.Font.Bold = true;
        entetes.Style.Font.FontColor = Blanc;
        entetes.Style.Fill.BackgroundColor = Rouge;
        entetes.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        entetes.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        feuille.Row(ligneEntetes).Height = 20;

        if (ligneFinDonnees >= ligneEntetes + 1)
        {
            var donnees = feuille.Range(ligneEntetes + 1, 1, ligneFinDonnees, nbColonnes);
            donnees.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            donnees.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            donnees.Style.Font.FontColor = Encre;

            for (var ligne = ligneEntetes + 1; ligne <= ligneFinDonnees; ligne++)
            {
                if ((ligne - ligneEntetes) % 2 == 0)
                    feuille.Range(ligne, 1, ligne, nbColonnes).Style.Fill.BackgroundColor = GrisTresClair;
            }
        }

        var tableauComplet = feuille.Range(ligneEntetes, 1, Math.Max(ligneFinDonnees, ligneEntetes), nbColonnes);
        tableauComplet.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableauComplet.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        tableauComplet.Style.Border.OutsideBorderColor = XLColor.FromHtml("#C0BFBF");
        tableauComplet.Style.Border.InsideBorderColor = XLColor.FromHtml("#C0BFBF");

        feuille.SheetView.FreezeRows(ligneEntetes);
        feuille.Columns(1, nbColonnes).AdjustToContents();
    }
}
