using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDOE.Infrastructure.Entities;

namespace PDOE.Infrastructure.Configurations;

public class ChecklistItemConfigConfiguration : IEntityTypeConfiguration<ChecklistItemConfig>
{
    public void Configure(EntityTypeBuilder<ChecklistItemConfig> builder)
    {
        builder.ToTable("ChecklistItemsConfig", t => t.HasCheckConstraint("CK_ChecklistItemsConfig_Ordre", "Ordre > 0"));
        builder.HasKey(c => c.ChecklistItemId);

        builder.Property(c => c.Libelle).HasMaxLength(300).IsRequired();
        builder.Property(c => c.Actif).HasDefaultValue(true);
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(c => c.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(c => c.CreatedBy).HasMaxLength(100).IsRequired();
        builder.Property(c => c.UpdatedBy).HasMaxLength(100).IsRequired();

        builder.HasIndex(c => c.Ordre).IsUnique();
        builder.HasIndex(c => new { c.Actif, c.Ordre });
    }
}
