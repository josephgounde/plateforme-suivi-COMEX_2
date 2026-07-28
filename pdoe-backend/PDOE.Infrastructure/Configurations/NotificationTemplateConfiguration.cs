using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDOE.Infrastructure.Entities;

namespace PDOE.Infrastructure.Configurations;

public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("NotificationTemplates", t => t.HasCheckConstraint(
            "CK_NotifTemplates_CanalDefaut", "CanalDefaut IN ('SMS', 'EMAIL', 'SMS_ET_EMAIL')"));

        // Pas d'IDENTITY : TypeEvenement est la clé primaire naturelle.
        builder.HasKey(n => n.TypeEvenement);
        builder.Property(n => n.TypeEvenement).HasMaxLength(100).ValueGeneratedNever();

        builder.Property(n => n.Libelle).HasMaxLength(150).IsRequired();
        builder.Property(n => n.Corps).HasMaxLength(1000).IsRequired();
        builder.Property(n => n.CanalDefaut).HasMaxLength(20).HasDefaultValue("EMAIL");
        builder.Property(n => n.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(n => n.UpdatedBy).HasMaxLength(100).IsRequired();
    }
}
