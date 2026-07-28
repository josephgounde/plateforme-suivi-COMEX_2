using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDOE.Infrastructure.Entities;

namespace PDOE.Infrastructure.Configurations;

public class PaiementPartielConfiguration : IEntityTypeConfiguration<PaiementPartiel>
{
    public void Configure(EntityTypeBuilder<PaiementPartiel> builder)
    {
        builder.ToTable("PaiementsPartiels", t =>
        {
            t.HasCheckConstraint("CK_Paiements_Montant", "MontantPaiement > 0");
            t.HasCheckConstraint("CK_Paiements_SoldeRestant", "SoldeRestant >= 0");
        });
        builder.HasKey(p => p.PaiementId);
        builder.HasIndex(p => new { p.DossierId, p.ReferencePaiement }).IsUnique();

        builder.Property(p => p.MontantPaiement).HasPrecision(18, 4);
        builder.Property(p => p.Devise).HasColumnType("nchar(3)").IsRequired();
        builder.Property(p => p.DatePaiement).HasColumnType("date");
        builder.Property(p => p.ReferencePaiement).HasMaxLength(100).IsRequired();
        builder.Property(p => p.SoldeRestant).HasPrecision(18, 4);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(p => p.CreatedBy).HasMaxLength(100).IsRequired();

        builder.HasOne(p => p.Dossier)
            .WithMany(d => d.PaiementsPartiels)
            .HasForeignKey(p => p.DossierId);
    }
}
