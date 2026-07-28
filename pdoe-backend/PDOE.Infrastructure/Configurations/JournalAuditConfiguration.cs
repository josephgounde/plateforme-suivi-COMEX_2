using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDOE.Infrastructure.Entities;

namespace PDOE.Infrastructure.Configurations;

public class JournalAuditConfiguration : IEntityTypeConfiguration<JournalAudit>
{
    public void Configure(EntityTypeBuilder<JournalAudit> builder)
    {
        builder.ToTable("JournalAudit");
        builder.HasKey(j => j.JournalAuditId);

        builder.Property(j => j.Categorie).HasMaxLength(30).IsRequired();
        builder.Property(j => j.TypeAction).HasMaxLength(50).IsRequired();
        builder.Property(j => j.Description).HasMaxLength(500).IsRequired();
        builder.Property(j => j.EntiteType).HasMaxLength(50);
        builder.Property(j => j.EntiteId).HasMaxLength(50);
        builder.Property(j => j.Succes).HasDefaultValue(true);
        builder.Property(j => j.DateAction).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(j => j.CreatedBy).HasMaxLength(100).IsRequired();

        // Pas de FK vers Utilisateurs — une ligne d'audit doit survivre à la
        // suppression/désactivation du compte qu'elle décrit.
    }
}
