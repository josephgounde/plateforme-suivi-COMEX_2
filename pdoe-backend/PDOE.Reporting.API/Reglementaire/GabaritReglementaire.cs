using System.Reflection;
using ClosedXML.Excel;

namespace PDOE.Reporting.API.Reglementaire;

/// <summary>Ouvre un gabarit officiel embarqué (cf. PDOE.Reporting.API.csproj) tel quel, prêt à être rempli.</summary>
public static class GabaritReglementaire
{
    public static XLWorkbook Ouvrir(string nomFichier)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var flux = assembly.GetManifestResourceStream($"PDOE.Reporting.API.ReglementaireTemplates.{nomFichier}")
            ?? throw new InvalidOperationException($"Gabarit embarqué introuvable : {nomFichier}");

        // Copie en mémoire obligatoire : ClosedXML garde une réf paresseuse au flux, qui serait déjà fermé au SaveAs sinon.
        var memoire = new MemoryStream();
        flux.CopyTo(memoire);
        memoire.Position = 0;
        return new XLWorkbook(memoire);
    }
}
