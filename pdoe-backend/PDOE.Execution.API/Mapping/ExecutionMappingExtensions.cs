using PDOE.Api.Contracts;
using PDOE.Infrastructure.Entities;

namespace PDOE.Execution.API.Mapping;

// Dupliqué de DossierMappingExtensions.ToResponse() (Dossiers.API) — modules indépendants, pas de ProjectReference.
public static class ExecutionMappingExtensions
{
    public static DossierResponse ToResponse(this Dossier d) => new()
    {
        DossierId = d.DossierId,
        ReferenceInterne = d.ReferenceInterne,
        NumCompte = d.NumCompte,
        NomClient = d.NomClient,
        TypeOperation = Enum.Parse<TypeOperation>(d.TypeOperation),
        Montant = (double)d.Montant,
        Devise = d.Devise,
        PaysBeneficiaire = d.PaysBeneficiaire,
        Motif = d.Motif,
        MatriculeClient = d.MatriculeClient,
        NomBeneficiaire = d.NomBeneficiaire,
        NatureTransaction = d.NatureTransaction,
        ReferenceDomiciliation = d.ReferenceDomiciliation,
        CodeStatistiqueOperateur = d.CodeStatistiqueOperateur,
        NifClient = d.NifClient,
        AdressePostaleClient = d.AdressePostaleClient,
        AdresseGeographiqueClient = d.AdresseGeographiqueClient,
        CodeBanque = d.CodeBanque,
        QualiteResidence = d.QualiteResidence is not null ? Enum.Parse<QualiteResidence>(d.QualiteResidence) : null,
        DateOuvertureCompte = d.DateOuvertureCompte?.ToDateTime(TimeOnly.MinValue),
        AnneeExerciceCompte = d.AnneeExerciceCompte,
        TypeCompteDebite = Enum.Parse<TypeCompte>(d.TypeCompteDebite),
        CodeSwiftIndicatif = d.CodeSwiftIndicatif,
        BanqueCorrespondanteIndicative = d.BanqueCorrespondanteIndicative,
        StatutElectronique = Enum.Parse<StatutDossier>(d.StatutElectronique),
        EtapeGenerique = d.EtapeGeneriqueCode is not null
            ? new EtapeGeneriqueInfo { EtapeCode = d.EtapeGeneriqueCode, SousEtat = Enum.Parse<SousEtat>(d.SousEtatGenerique!) }
            : null,
        GestionnaireAssigne = d.GestionnaireAssigneLogin,
        ModeVerificationApplique = Enum.Parse<ModeVerificationSignature>(d.ModeVerificationApplique),
        SignatureVerifieeVisuellement = d.SignatureVerifieeVisuellement,
        InitialesAgent = d.InitialesAgent,
        DateConfirmationClient = d.DateConfirmationClient,
        SoldeCompteVerifie = d.SoldeCompteVerifie,
        EmailClient = d.EmailClient,
        TelephoneClient = d.TelephoneClient,
        TauxChange = (double?)d.TauxChange,
        DeviseCotation = d.DeviseCotation,
        CorrespondantDesigne = d.CorrespondantDesigne,
        BicCorrespondant = d.BicCorrespondant,
        DateDebit = d.DateDebit?.ToDateTime(TimeOnly.MinValue),
        Couverture = d.Couverture,
        DisponibiliteFonds = d.DisponibiliteFonds,
        ReferenceABS = d.ReferenceABS,
        ReferenceSWIFT = d.ReferenceSWIFT,
        NumeroAC = d.NumeroAC,
        CodeTRF = d.CodeTRF,
        DateExecution = d.DateExecution,
        MontantExecute = (double?)d.MontantExecute,
        DateEcheanceApurement = d.DateEcheanceApurement?.ToDateTime(TimeOnly.MinValue),
        SoldeRestantApurement = (double?)d.SoldeRestantApurement,
        ApurementComplet = d.ApurementComplet,
        UpdatedAt = d.UpdatedAt,
        UpdatedBy = d.UpdatedBy,
        DateDerniereAction = d.EtapesWorkflow.Count > 0
            ? d.EtapesWorkflow.Max(e => e.DateAction)
            : d.UpdatedAt,
    };
}
