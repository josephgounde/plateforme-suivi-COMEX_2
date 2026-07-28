using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDOE.Infrastructure.Entities;

namespace PDOE.Infrastructure.Configurations;

public class AlerteApurementConfiguration : IEntityTypeConfiguration<AlerteApurement>
{
    public void Configure(EntityTypeBuilder<AlerteApurement> builder)
    {
        builder.ToTable("AlertesApurement", t => t.HasCheckConstraint("CK_Alertes_JRestants", "JRestants >= 0"));
        builder.HasKey(a => a.AlerteId);

        builder.Property(a => a.TypeAlerte).HasMaxLength(30).IsRequired();
        builder.Property(a => a.Envoye).HasDefaultValue(false);
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(a => a.CreatedBy).HasMaxLength(100).IsRequired();

        builder.HasIndex(a => new { a.DossierId, a.TypeAlerte }).IsUnique();

        builder.HasOne(a => a.Dossier)
            .WithMany(d => d.Alertes)
            .HasForeignKey(a => a.DossierId);
    }
}
