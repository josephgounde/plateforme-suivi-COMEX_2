using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PDOE.Infrastructure.Entities;

namespace PDOE.Infrastructure.Configurations;

public class DossierConfiguration : IEntityTypeConfiguration<Dossier>
{
    public void Configure(EntityTypeBuilder<Dossier> builder)
    {
        builder.ToTable("Dossiers", t =>
        {
            t.HasCheckConstraint("CK_Dossiers_Montant", "Montant > 0");
            t.HasCheckConstraint("CK_Dossiers_MontantExecute", "MontantExecute IS NULL OR MontantExecute > 0");
            t.HasCheckConstraint("CK_Dossiers_ModeVerification", "ModeVerificationApplique IN ('AUTOMATIQUE', 'VISUEL', 'LES_DEUX')");
            t.HasCheckConstraint("CK_Dossiers_TypeOperation", "TypeOperation IN ('IMPORT_BIENS', 'IMPORT_SERVICES', 'EXPORT_BIENS', 'EXPORT_SERVICES', 'TRANSFERT_CAPITAUX')");
            t.HasCheckConstraint("CK_Dossiers_TypeCompteDebite", "TypeCompteDebite IN ('COURANT', 'EPARGNE', 'DEVISE')");
            t.HasCheckConstraint("CK_Dossiers_QualiteResidence", "QualiteResidence IS NULL OR QualiteResidence IN ('RESIDENT', 'NON_RESIDENT')");
            t.HasCheckConstraint("CK_Dossiers_SignatureDateCoherence", "SignatureValideeABS = 0 OR DateValidationSignature IS NOT NULL");
            t.HasCheckConstraint("CK_Dossiers_VerifVisuelleCoherence", "SignatureVerifieeVisuellement = 0 OR InitialesAgent IS NOT NULL");
            t.HasCheckConstraint("CK_Dossiers_ExecutionApurementCoherence", "DateExecution IS NULL OR DateEcheanceApurement IS NOT NULL");
            t.HasCheckConstraint("CK_Dossiers_SousEtatGenerique", "SousEtatGenerique IS NULL OR SousEtatGenerique IN ('EN_ATTENTE', 'VALIDE', 'REJETE')");
            t.HasCheckConstraint("CK_Dossiers_EtapeGeneriqueCoherence",
                "(EtapeGeneriqueCode IS NULL AND SousEtatGenerique IS NULL) OR (EtapeGeneriqueCode IS NOT NULL AND SousEtatGenerique IS NOT NULL)");
        });
        builder.HasKey(d => d.DossierId);

        builder.Property(d => d.ReferenceInterne).HasMaxLength(30).IsRequired();
        builder.Property(d => d.NumCompte).HasMaxLength(20).IsRequired();
        builder.Property(d => d.NomClient).HasMaxLength(150).IsRequired();
        builder.Property(d => d.TypeOperation).HasMaxLength(20).IsRequired();
        builder.Property(d => d.Montant).HasPrecision(18, 4);
        builder.Property(d => d.Devise).HasColumnType("nchar(3)").IsRequired();
        builder.Property(d => d.PaysBeneficiaire).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Motif).HasMaxLength(500).IsRequired();

        builder.Property(d => d.MatriculeClient).HasMaxLength(50).IsRequired();
        builder.Property(d => d.NomBeneficiaire).HasMaxLength(150).IsRequired();
        builder.Property(d => d.NatureTransaction).HasMaxLength(200).IsRequired();
        builder.Property(d => d.ReferenceDomiciliation).HasMaxLength(50);
        builder.Property(d => d.CodeStatistiqueOperateur).HasMaxLength(50);
        builder.Property(d => d.NifClient).HasMaxLength(50);
        builder.Property(d => d.AdressePostaleClient).HasMaxLength(250);
        builder.Property(d => d.AdresseGeographiqueClient).HasMaxLength(250);
        builder.Property(d => d.CodeBanque).HasMaxLength(10);
        builder.Property(d => d.QualiteResidence).HasMaxLength(20);
        builder.Property(d => d.DateOuvertureCompte).HasColumnType("date");
        builder.Property(d => d.TypeCompteDebite).HasMaxLength(20).IsRequired();
        builder.Property(d => d.CodeSwiftIndicatif).HasMaxLength(11);
        builder.Property(d => d.BanqueCorrespondanteIndicative).HasMaxLength(200);

        builder.Property(d => d.StatutElectronique).HasMaxLength(50).IsRequired();
        builder.Property(d => d.EtapeGeneriqueCode).HasMaxLength(30);
        builder.Property(d => d.SousEtatGenerique).HasMaxLength(20);

        builder.Property(d => d.GestionnaireAssigneLogin).HasMaxLength(100);

        builder.Property(d => d.SignatureValideeABS).HasDefaultValue(false);
        builder.Property(d => d.ModeVerificationApplique).HasMaxLength(20).HasDefaultValue("AUTOMATIQUE");
        builder.Property(d => d.SignatureVerifieeVisuellement).HasDefaultValue(false);
        builder.Property(d => d.InitialesAgent).HasMaxLength(10);

        builder.Property(d => d.SoldeCompteVerifie).HasDefaultValue(false);
        builder.Property(d => d.SoldeConstate).HasPrecision(18, 4);
        builder.Property(d => d.DeviseConstatee).HasColumnType("nchar(3)");
        builder.Property(d => d.EmailClient).HasMaxLength(150);
        builder.Property(d => d.TelephoneClient).HasMaxLength(30);
        builder.Property(d => d.TauxChange).HasPrecision(18, 6);
        builder.Property(d => d.DeviseCotation).HasColumnType("nchar(3)");
        builder.Property(d => d.CorrespondantDesigne).HasMaxLength(200);
        builder.Property(d => d.BicCorrespondant).HasColumnType("nchar(11)");
        builder.Property(d => d.DateDebit).HasColumnType("date");
        builder.Property(d => d.Couverture).HasMaxLength(200);
        builder.Property(d => d.DisponibiliteFonds).HasDefaultValue(false);

        builder.Property(d => d.ReferenceABS).HasMaxLength(50);
        builder.Property(d => d.ReferenceSWIFT).HasMaxLength(50);
        builder.Property(d => d.NumeroAC).HasMaxLength(30);
        builder.Property(d => d.CodeTRF).HasMaxLength(30);
        builder.Property(d => d.MontantExecute).HasPrecision(18, 4);

        builder.Property(d => d.DateEcheanceApurement).HasColumnType("date");
        builder.Property(d => d.SoldeRestantApurement).HasPrecision(18, 4);
        builder.Property(d => d.ApurementComplet).HasDefaultValue(false);

        builder.Property(d => d.NotifieArchivage).HasDefaultValue(false);
        builder.Property(d => d.ArchivageConfirme).HasDefaultValue(false);

        builder.Property(d => d.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(d => d.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        builder.Property(d => d.CreatedBy).HasMaxLength(100).IsRequired();
        builder.Property(d => d.UpdatedBy).HasMaxLength(100).IsRequired();

        builder.HasIndex(d => d.ReferenceInterne).IsUnique();
        builder.HasIndex(d => d.NumCompte);
        builder.HasIndex(d => d.StatutElectronique)
            .IncludeProperties(d => new { d.DossierId, d.ReferenceInterne, d.NomClient, d.UpdatedAt });
    }
}
