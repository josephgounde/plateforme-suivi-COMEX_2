using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDOE.Infrastructure.Entities;

namespace PDOE.Infrastructure.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.NotificationId);

        builder.Property(n => n.TypeEvenement).HasMaxLength(100).IsRequired();
        builder.Property(n => n.Canal).HasMaxLength(20).IsRequired();
        builder.Property(n => n.Destinataire).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Sujet).HasMaxLength(300);
        builder.Property(n => n.Corps).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(n => n.MessageIdGateway).HasMaxLength(100);
        builder.Property(n => n.Statut).HasMaxLength(20).HasDefaultValue("EN_ATTENTE");
        builder.Property(n => n.CodeErreur).HasMaxLength(50);
        builder.Property(n => n.NbTentatives).HasDefaultValue(0);
        builder.Property(n => n.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(n => n.CreatedBy).HasMaxLength(100).IsRequired();

        builder.HasOne(n => n.Dossier)
            .WithMany()
            .HasForeignKey(n => n.DossierId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
