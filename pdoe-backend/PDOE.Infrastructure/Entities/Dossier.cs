namespace PDOE.Infrastructure.Entities;

public class Dossier
{
    public int DossierId { get; set; }

    /// <summary>N° interne (PDOE-{yyyyMM}-{seq}), attribué à la création — distinct de ReferenceSWIFT, la vraie référence COMEX du processus physique, attribuée à la déclaration d'exécution.</summary>
    public string ReferenceInterne { get; set; } = null!;
    public string NumCompte { get; set; } = null!;
    public string NomClient { get; set; } = null!;

    /// <summary>IMPORT | EXPORT | SERVICE | TRANSFERT</summary>
    public string TypeOperation { get; set; } = null!;
    public decimal Montant { get; set; }
    public string Devise { get; set; } = null!;
    public string PaysBeneficiaire { get; set; } = null!;
    public string Motif { get; set; } = null!;

    public string MatriculeClient { get; set; } = null!;
    public string NomBeneficiaire { get; set; } = null!;
    public string NatureTransaction { get; set; } = null!;

    /// <summary>Référence de domiciliation réglementaire — distincte de ReferenceInterne (n° interne PDOE) et de ReferenceSWIFT (attribuée à l'exécution).</summary>
    public string? ReferenceDomiciliation { get; set; }
    /// <summary>Code statistique / n° d'identification de l'opérateur économique (client).</summary>
    public string? CodeStatistiqueOperateur { get; set; }
    /// <summary>NIF/NCC du client — distinct de MatriculeClient (identifiant interne AFB).</summary>
    public string? NifClient { get; set; }

    public string? AdressePostaleClient { get; set; }
    public string? AdresseGeographiqueClient { get; set; }
    /// <summary>Code banque (Afriland First Bank CI).</summary>
    public string? CodeBanque { get; set; }
    /// <summary>RESIDENT | NON_RESIDENT</summary>
    public string? QualiteResidence { get; set; }
    public DateOnly? DateOuvertureCompte { get; set; }
    public int? AnneeExerciceCompte { get; set; }

    /// <summary>COURANT | EPARGNE | DEVISE</summary>
    public string TypeCompteDebite { get; set; } = null!;

    /// <summary>Indicatif préliminaire (Agent d'accueil) — distinct de CorrespondantDesigne/BicCorrespondant (Trésorerie, étape 4).</summary>
    public string? CodeSwiftIndicatif { get; set; }
    public string? BanqueCorrespondanteIndicative { get; set; }

    public string StatutElectronique { get; set; } = null!;

    /// <summary>Renseigné uniquement sur une étape GENERIQUE — StatutElectronique reste figé pendant ce temps.</summary>
    public string? EtapeGeneriqueCode { get; set; }

    /// <summary>EN_ATTENTE | VALIDE | REJETE — cf. EtapeGeneriqueCode.</summary>
    public string? SousEtatGenerique { get; set; }

    /// <summary>Si renseigné par l'Admin, prime sur le portefeuille automatique.</summary>
    public string? GestionnaireAssigneLogin { get; set; }

    public bool SignatureValideeABS { get; set; }
    public DateTime? DateValidationSignature { get; set; }

    /// <summary>AUTOMATIQUE | VISUEL | LES_DEUX</summary>
    public string ModeVerificationApplique { get; set; } = "AUTOMATIQUE";
    public bool SignatureVerifieeVisuellement { get; set; }
    public string? InitialesAgent { get; set; }

    public DateTime? DateConfirmationClient { get; set; }
    public bool SoldeCompteVerifie { get; set; }

    /// <summary>Pas de lookup CBS pour EmailClient en v1 — alimenté uniquement par le seed pour l'instant.</summary>
    public string? EmailClient { get; set; }
    public string? TelephoneClient { get; set; }
    public decimal? TauxChange { get; set; }
    public string? DeviseCotation { get; set; }
    public string? CorrespondantDesigne { get; set; }
    public string? BicCorrespondant { get; set; }
    public DateOnly? DateDebit { get; set; }
    public string? Couverture { get; set; }
    public bool DisponibiliteFonds { get; set; }

    public string? ReferenceABS { get; set; }
    public string? ReferenceSWIFT { get; set; }
    /// <summary>N° de l'Attestation/Autorisation de Change (AC) — saisi par l'Agent COMEX à la déclaration d'exécution.</summary>
    public string? NumeroAC { get; set; }
    /// <summary>Code TRF (formulaire de change) — saisi par l'Agent COMEX à la déclaration d'exécution.</summary>
    public string? CodeTRF { get; set; }
    public DateTime? DateExecution { get; set; }
    public decimal? MontantExecute { get; set; }

    public DateOnly? DateEcheanceApurement { get; set; }
    public decimal? SoldeRestantApurement { get; set; }
    public bool ApurementComplet { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string UpdatedBy { get; set; } = null!;

    public Utilisateur? GestionnaireAssigne { get; set; }
    public ICollection<EtapeWorkflow> EtapesWorkflow { get; set; } = new List<EtapeWorkflow>();
    public ICollection<PaiementPartiel> PaiementsPartiels { get; set; } = new List<PaiementPartiel>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<AlerteApurement> Alertes { get; set; } = new List<AlerteApurement>();
}
