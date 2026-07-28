using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDOE.Infrastructure.Entities;

namespace PDOE.Infrastructure.Configurations;

public class UtilisateurConfiguration : IEntityTypeConfiguration<Utilisateur>
{
    public void Configure(EntityTypeBuilder<Utilisateur> builder)
    {
        builder.ToTable("Utilisateurs", t => t.HasCheckConstraint(
            "CK_Utilisateurs_Profil",
            "Profil IN ('AGENT_ACCUEIL', 'GESTIONNAIRE', 'AGENT_COMEX', 'TRESORERIE', 'DIRECTION', 'ADMIN_DSIRI', 'SUPER_ADMIN')"));
        builder.HasKey(u => u.UtilisateurId);

        builder.Property(u => u.LoginAD).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Nom).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Prenom).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(150).IsRequired();
        builder.Property(u => u.Profil).HasMaxLength(30).IsRequired();
        builder.Property(u => u.EstActif).HasDefaultValue(true);
        builder.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(u => u.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(u => u.CreatedBy).HasMaxLength(100).IsRequired();
        builder.Property(u => u.UpdatedBy).HasMaxLength(100).IsRequired();

        // Utilisateurs(LoginAD) est la cible des FK Dossiers/GestionnaireClients/
        // EtapesWorkflow — clé alternative en plus de la PK UtilisateurId.
        builder.HasAlternateKey(u => u.LoginAD);

        builder.HasMany(u => u.Portefeuille)
            .WithOne(g => g.Gestionnaire)
            .HasForeignKey(g => g.GestionnaireLogin)
            .HasPrincipalKey(u => u.LoginAD)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.DossiersAssignes)
            .WithOne(d => d.GestionnaireAssigne)
            .HasForeignKey(d => d.GestionnaireAssigneLogin)
            .HasPrincipalKey(u => u.LoginAD)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
