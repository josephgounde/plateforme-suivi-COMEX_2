using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDOE.Infrastructure.Entities;

namespace PDOE.Infrastructure.Configurations;

public class WorkflowEtapeConfiguration : IEntityTypeConfiguration<WorkflowEtape>
{
    public void Configure(EntityTypeBuilder<WorkflowEtape> builder)
    {
        builder.ToTable("WorkflowEtapes", t =>
        {
            t.HasCheckConstraint("CK_WorkflowEtapes_TypeEtape",
                "TypeEtape IN ('GESTIONNAIRE', 'COMEX', 'TRESORERIE', 'EXECUTION', 'APUREMENT', 'GENERIQUE')");
            t.HasCheckConstraint("CK_WorkflowEtapes_Ordre", "Ordre > 0");
        });
        builder.HasKey(w => w.EtapeConfigId);

        builder.Property(w => w.Code).HasMaxLength(30).IsRequired();
        builder.Property(w => w.Libelle).HasMaxLength(100).IsRequired();
        builder.Property(w => w.Actif).HasDefaultValue(true);
        builder.Property(w => w.TypeEtape).HasMaxLength(20).IsRequired();
        builder.Property(w => w.Description).HasMaxLength(500);
        builder.Property(w => w.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(w => w.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(w => w.CreatedBy).HasMaxLength(100).IsRequired();
        builder.Property(w => w.UpdatedBy).HasMaxLength(100).IsRequired();

        builder.HasIndex(w => w.Code).IsUnique();
        builder.HasIndex(w => w.Ordre).IsUnique();
        builder.HasIndex(w => new { w.Actif, w.Ordre });
    }
}
