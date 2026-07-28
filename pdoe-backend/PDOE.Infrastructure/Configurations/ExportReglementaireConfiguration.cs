using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDOE.Infrastructure.Entities;

namespace PDOE.Infrastructure.Configurations;

public class ExportReglementaireConfiguration : IEntityTypeConfiguration<ExportReglementaire>
{
    public void Configure(EntityTypeBuilder<ExportReglementaire> builder)
    {
        builder.ToTable("ExportsReglementaires", t =>
        {
            t.HasCheckConstraint("CK_ExportsReglementaires_Taille", "TailleFichier > 0");
            t.HasCheckConstraint("CK_ExportsReglementaires_Categorie", "Categorie IN ('REGLEMENTAIRE', 'OPERATIONNEL')");
        });
        builder.HasKey(e => e.ExportReglementaireId);

        builder.Property(e => e.Categorie).HasMaxLength(20).IsRequired();
        builder.Property(e => e.TypeExport).HasMaxLength(30).IsRequired();
        builder.Property(e => e.NomFichier).HasMaxLength(255).IsRequired();
        builder.Property(e => e.CheminFichier).HasMaxLength(500).IsRequired();
        builder.Property(e => e.HashSHA256).HasColumnType("nchar(64)").IsRequired();
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(e => e.CreatedBy).HasMaxLength(100).IsRequired();

        // Pas de FK — indépendante des Dossiers (couvre une période, pas un
        // dossier précis), même raisonnement que JournalAudit pour CreatedBy.
    }
}
