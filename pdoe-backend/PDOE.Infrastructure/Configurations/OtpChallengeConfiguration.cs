using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDOE.Infrastructure.Entities;

namespace PDOE.Infrastructure.Configurations;

public class OtpChallengeConfiguration : IEntityTypeConfiguration<OtpChallenge>
{
    public void Configure(EntityTypeBuilder<OtpChallenge> builder)
    {
        builder.ToTable("OtpChallenges");
        builder.HasKey(o => o.OtpToken);

        builder.Property(o => o.OtpToken).HasMaxLength(100);
        builder.Property(o => o.LoginAD).HasMaxLength(100).IsRequired();
        builder.Property(o => o.Code).HasColumnType("nchar(6)").IsRequired();
        builder.Property(o => o.NomComplet).HasMaxLength(200).IsRequired();
        builder.Property(o => o.Email).HasMaxLength(200).IsRequired();
        builder.Property(o => o.Profil).HasMaxLength(30).IsRequired();
        builder.Property(o => o.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

        // Pas de FK vers Utilisateurs — même raisonnement que JournalAudit : ligne transitoire,
        // ne doit pas dépendre du cycle de vie du compte.
    }
}
