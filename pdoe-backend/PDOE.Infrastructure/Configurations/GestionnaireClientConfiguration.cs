using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDOE.Infrastructure.Entities;

namespace PDOE.Infrastructure.Configurations;

public class GestionnaireClientConfiguration : IEntityTypeConfiguration<GestionnaireClient>
{
    public void Configure(EntityTypeBuilder<GestionnaireClient> builder)
    {
        builder.ToTable("GestionnaireClients");
        builder.HasKey(g => g.GestionnaireClientId);

        builder.Property(g => g.GestionnaireLogin).HasMaxLength(100).IsRequired();
        builder.Property(g => g.NumCompte).HasMaxLength(20).IsRequired();
        builder.Property(g => g.DateAffectation).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(g => g.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(g => g.CreatedBy).HasMaxLength(100).IsRequired();

        builder.HasIndex(g => new { g.GestionnaireLogin, g.NumCompte }).IsUnique();
        builder.HasIndex(g => g.GestionnaireLogin).IncludeProperties(g => g.NumCompte);

        // FK -> Utilisateurs(LoginAD) configurée côté UtilisateurConfiguration
        // (HasMany(u => u.Portefeuille)...) pour éviter une double déclaration.
    }
}
