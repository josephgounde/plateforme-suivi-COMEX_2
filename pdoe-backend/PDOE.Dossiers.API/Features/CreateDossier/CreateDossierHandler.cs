using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Dossiers.API.Mapping;
using PDOE.Infrastructure;
using PDOE.Infrastructure.Entities;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Dossiers.API.Features.CreateDossier;

public class CreateDossierHandler(PdoeDbContext db) : IRequestHandler<CreateDossierCommand, DossierResponse>
{
    public async Task<DossierResponse> Handle(CreateDossierCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        if (!request.SignatureValideeABS)
        {
            throw new DomainException(422, ErrorResponseCode.SIGNATURE_INVALIDE,
                "La signature doit être validée via GET /clients/{numCompte}/verifier-signature avant la création du dossier.");
        }

        var now = DateTime.UtcNow;
        var referenceInterne = await GenererReferenceInterneAsync(now, cancellationToken);

        var gestionnaireLogin = await db.GestionnaireClients
            .Where(g => g.NumCompte == request.NumCompte)
            .Select(g => g.GestionnaireLogin)
            .FirstOrDefaultAsync(cancellationToken);

        var dossier = new Dossier
        {
            ReferenceInterne = referenceInterne,
            NumCompte = request.NumCompte,
            NomClient = request.NomClient,
            TypeOperation = request.TypeOperation.ToString(),
            Montant = request.Montant,
            Devise = request.Devise,
            PaysBeneficiaire = request.PaysBeneficiaire,
            Motif = request.Motif,
            MatriculeClient = request.MatriculeClient,
            NomBeneficiaire = request.NomBeneficiaire,
            NatureTransaction = request.NatureTransaction,
            ReferenceDomiciliation = request.ReferenceDomiciliation,
            CodeStatistiqueOperateur = request.CodeStatistiqueOperateur,
            NifClient = request.NifClient,
            AdressePostaleClient = request.AdressePostaleClient,
            AdresseGeographiqueClient = request.AdresseGeographiqueClient,
            TelephoneClient = request.TelephoneClient,
            CodeBanque = request.CodeBanque,
            QualiteResidence = request.QualiteResidence?.ToString(),
            // .Date (pas .UtcDateTime) : cf. commentaire équivalent dans CreerPaiementHandler.
            DateOuvertureCompte = request.DateOuvertureCompte is not null ? DateOnly.FromDateTime(request.DateOuvertureCompte.Value.Date) : null,
            AnneeExerciceCompte = request.AnneeExerciceCompte,
            TypeCompteDebite = request.TypeCompteDebite.ToString(),
            CodeSwiftIndicatif = request.CodeSwiftIndicatif,
            BanqueCorrespondanteIndicative = request.BanqueCorrespondanteIndicative,
            StatutElectronique = nameof(StatutDossier.BROUILLON),
            GestionnaireAssigneLogin = gestionnaireLogin,
            SignatureValideeABS = request.SignatureValideeABS,
            DateValidationSignature = request.DateValidationSignature.UtcDateTime,
            ModeVerificationApplique = request.ModeVerificationApplique.ToString(),
            SignatureVerifieeVisuellement = request.SignatureVerifieeVisuellement,
            InitialesAgent = request.InitialesAgent,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = CurrentUser.Login,
            UpdatedBy = CurrentUser.Login,
        };

        db.Dossiers.Add(dossier);
        await db.SaveChangesAsync(cancellationToken);

        return dossier.ToResponse();
    }

    /// PDOE-{yyyyMM}-{seq 4 chiffres}, compteur remis à 1 chaque mois. Pas la ReferenceSWIFT
    /// (celle-là vient plus tard, cf. DeclarerExecutionHandler).
    private async Task<string> GenererReferenceInterneAsync(DateTime now, CancellationToken cancellationToken)
    {
        var prefix = $"PDOE-{now:yyyyMM}-";
        var count = await db.Dossiers.CountAsync(d => d.ReferenceInterne.StartsWith(prefix), cancellationToken);
        return $"{prefix}{count + 1:D4}";
    }
}
