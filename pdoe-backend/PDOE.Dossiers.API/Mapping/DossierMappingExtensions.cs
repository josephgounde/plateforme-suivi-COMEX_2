using PDOE.Api.Contracts;
using PDOE.Infrastructure.Entities;

namespace PDOE.Dossiers.API.Mapping;

public static class DossierMappingExtensions
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
        SoldeSuffisant = d.SoldeSuffisant,
        SoldeConstate = d.SoldeConstate,
        DeviseConstatee = d.DeviseConstatee,
        DateVerificationSolde = d.DateVerificationSolde,
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

    public static DossierDetailResponse ToDetailResponse(this Dossier d)
    {
        var response = d.ToResponse();
        return new DossierDetailResponse
        {
            DossierId = response.DossierId,
            ReferenceInterne = response.ReferenceInterne,
            NumCompte = response.NumCompte,
            NomClient = response.NomClient,
            TypeOperation = response.TypeOperation,
            Montant = response.Montant,
            Devise = response.Devise,
            PaysBeneficiaire = response.PaysBeneficiaire,
            Motif = response.Motif,
            MatriculeClient = response.MatriculeClient,
            NomBeneficiaire = response.NomBeneficiaire,
            NatureTransaction = response.NatureTransaction,
            ReferenceDomiciliation = response.ReferenceDomiciliation,
            CodeStatistiqueOperateur = response.CodeStatistiqueOperateur,
            NifClient = response.NifClient,
            AdressePostaleClient = response.AdressePostaleClient,
            AdresseGeographiqueClient = response.AdresseGeographiqueClient,
            CodeBanque = response.CodeBanque,
            QualiteResidence = response.QualiteResidence,
            DateOuvertureCompte = response.DateOuvertureCompte,
            AnneeExerciceCompte = response.AnneeExerciceCompte,
            TypeCompteDebite = response.TypeCompteDebite,
            CodeSwiftIndicatif = response.CodeSwiftIndicatif,
            BanqueCorrespondanteIndicative = response.BanqueCorrespondanteIndicative,
            StatutElectronique = response.StatutElectronique,
            EtapeGenerique = response.EtapeGenerique,
            GestionnaireAssigne = response.GestionnaireAssigne,
            ModeVerificationApplique = response.ModeVerificationApplique,
            SignatureVerifieeVisuellement = response.SignatureVerifieeVisuellement,
            InitialesAgent = response.InitialesAgent,
            DateConfirmationClient = response.DateConfirmationClient,
            SoldeCompteVerifie = response.SoldeCompteVerifie,
            SoldeSuffisant = response.SoldeSuffisant,
            SoldeConstate = response.SoldeConstate,
            DeviseConstatee = response.DeviseConstatee,
            DateVerificationSolde = response.DateVerificationSolde,
            EmailClient = response.EmailClient,
            TelephoneClient = response.TelephoneClient,
            TauxChange = response.TauxChange,
            DeviseCotation = response.DeviseCotation,
            CorrespondantDesigne = response.CorrespondantDesigne,
            BicCorrespondant = response.BicCorrespondant,
            DateDebit = response.DateDebit,
            Couverture = response.Couverture,
            DisponibiliteFonds = response.DisponibiliteFonds,
            ReferenceABS = response.ReferenceABS,
            ReferenceSWIFT = response.ReferenceSWIFT,
            NumeroAC = response.NumeroAC,
            CodeTRF = response.CodeTRF,
            DateExecution = response.DateExecution,
            MontantExecute = response.MontantExecute,
            DateEcheanceApurement = response.DateEcheanceApurement,
            SoldeRestantApurement = response.SoldeRestantApurement,
            ApurementComplet = response.ApurementComplet,
            UpdatedAt = response.UpdatedAt,
            UpdatedBy = response.UpdatedBy,
            DateDerniereAction = response.DateDerniereAction,
            Documents = d.Documents.Select(doc => doc.ToResponse()).ToList(),
            EtapesWorkflow = d.EtapesWorkflow
                .OrderBy(e => e.DateAction)
                .Select(e => e.ToResponse())
                .ToList(),
            PaiementsPartiels = d.PaiementsPartiels.Select(p => p.ToResponse()).ToList(),
            Alertes = d.Alertes.Select(a => a.ToResponse()).ToList(),
        };
    }

    public static DocumentResponse ToResponse(this Document doc) => new()
    {
        DocumentId = doc.DocumentId,
        TypeDocument = Enum.Parse<TypeDocument>(doc.TypeDocument),
        ReferenceDocument = doc.ReferenceDocument,
        NomFichier = doc.NomFichier,
        HashSHA256 = doc.HashSHA256,
        TailleFichier = doc.TailleFichier,
        EstObligatoire = doc.EstObligatoire,
        EstValide = doc.EstValide,
        CreatedAt = doc.CreatedAt,
        CreatedBy = doc.CreatedBy,
    };

    public static EtapeWorkflowResponse ToResponse(this EtapeWorkflow e) => new()
    {
        EtapeId = e.EtapeId,
        NiveauValidation = e.NiveauValidation,
        StatutAvant = Enum.Parse<StatutDossier>(e.StatutAvant),
        StatutApres = Enum.Parse<StatutDossier>(e.StatutApres),
        Action = Enum.Parse<ActionWorkflow>(e.Action),
        MotifRejet = e.MotifRejet,
        ResponsableCorrection = e.ResponsableCorrection,
        AgentLogin = e.AgentLogin,
        DateAction = e.DateAction,
    };

    public static PaiementResponse ToResponse(this PaiementPartiel p) => new()
    {
        PaiementId = p.PaiementId,
        MontantPaiement = (double)p.MontantPaiement,
        Devise = p.Devise,
        DatePaiement = p.DatePaiement.ToDateTime(TimeOnly.MinValue),
        ReferencePaiement = p.ReferencePaiement,
        SoldeRestant = (double)p.SoldeRestant,
        CreatedAt = p.CreatedAt,
    };

    public static AlerteApurementResponse ToResponse(this AlerteApurement a) => new()
    {
        AlerteId = a.AlerteId,
        TypeAlerte = Enum.Parse<TypeAlerte>(a.TypeAlerte),
        JRestants = a.JRestants,
        DateAlerte = a.DateAlerte,
        Envoye = a.Envoye,
        DateEnvoi = a.DateEnvoi,
    };
}
