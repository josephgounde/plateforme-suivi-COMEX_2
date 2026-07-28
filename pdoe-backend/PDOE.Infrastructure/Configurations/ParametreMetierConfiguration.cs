using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDOE.Infrastructure.Entities;

namespace PDOE.Infrastructure.Configurations;

public class ParametreMetierConfiguration : IEntityTypeConfiguration<ParametreMetier>
{
    public void Configure(EntityTypeBuilder<ParametreMetier> builder)
    {
        builder.ToTable("ParametrageMetier");
        builder.HasKey(p => p.ParametreId);

        builder.Property(p => p.Cle).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Valeur).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Unite).HasMaxLength(30).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(500).IsRequired();
        builder.Property(p => p.ModifiableUI).HasDefaultValue(true);
        builder.Property(p => p.ValeurMin).HasMaxLength(50);
        builder.Property(p => p.ValeurMax).HasMaxLength(50);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(p => p.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(p => p.CreatedBy).HasMaxLength(100).IsRequired();
        builder.Property(p => p.UpdatedBy).HasMaxLength(100).IsRequired();

        builder.HasIndex(p => p.Cle).IsUnique();
    }
}
