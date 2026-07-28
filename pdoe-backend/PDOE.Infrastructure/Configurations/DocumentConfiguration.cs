using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDOE.Infrastructure.Entities;

namespace PDOE.Infrastructure.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents", t => t.HasCheckConstraint("CK_Documents_Taille", "TailleFichier > 0"));
        builder.HasKey(d => d.DocumentId);

        builder.Property(d => d.TypeDocument).HasMaxLength(30).IsRequired();
        builder.Property(d => d.ReferenceDocument).HasMaxLength(100);
        builder.Property(d => d.NomFichier).HasMaxLength(255).IsRequired();
        builder.Property(d => d.CheminIIS).HasMaxLength(500).IsRequired();
        builder.Property(d => d.HashSHA256).HasColumnType("nchar(64)").IsRequired();
        builder.Property(d => d.EstObligatoire).HasDefaultValue(false);
        builder.Property(d => d.EstValide).HasDefaultValue(false);
        builder.Property(d => d.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(d => d.CreatedBy).HasMaxLength(100).IsRequired();

        builder.HasOne(d => d.Dossier)
            .WithMany(d => d.Documents)
            .HasForeignKey(d => d.DossierId);

        builder.HasOne(d => d.Paiement)
            .WithMany(p => p.Documents)
            .HasForeignKey(d => d.PaiementId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
