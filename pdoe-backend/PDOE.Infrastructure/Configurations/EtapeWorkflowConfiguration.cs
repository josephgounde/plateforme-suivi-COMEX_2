using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDOE.Infrastructure.Entities;

namespace PDOE.Infrastructure.Configurations;

public class EtapeWorkflowConfiguration : IEntityTypeConfiguration<EtapeWorkflow>
{
    public void Configure(EntityTypeBuilder<EtapeWorkflow> builder)
    {
        builder.ToTable("EtapesWorkflow", t =>
        {
            t.HasCheckConstraint("CK_EtapesWF_MotifRejet", "Action <> 'REJET' OR MotifRejet IS NOT NULL");
            t.HasCheckConstraint("CK_EtapesWF_ResponsableCorrection", "Action <> 'REJET' OR ResponsableCorrection IS NOT NULL");
        });
        builder.HasKey(e => e.EtapeId);

        builder.Property(e => e.NiveauValidation).HasMaxLength(30).IsRequired();
        builder.Property(e => e.StatutAvant).HasMaxLength(50).IsRequired();
        builder.Property(e => e.StatutApres).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Action).HasMaxLength(30).IsRequired();
        builder.Property(e => e.MotifRejet).HasMaxLength(1000);
        builder.Property(e => e.ResponsableCorrection).HasMaxLength(200);
        builder.Property(e => e.AgentLogin).HasMaxLength(100).IsRequired();
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(e => e.CreatedBy).HasMaxLength(100).IsRequired();

        builder.HasOne(e => e.Dossier)
            .WithMany(d => d.EtapesWorkflow)
            .HasForeignKey(e => e.DossierId);

        builder.HasOne(e => e.Agent)
            .WithMany()
            .HasForeignKey(e => e.AgentLogin)
            .HasPrincipalKey(u => u.LoginAD);
    }
}
