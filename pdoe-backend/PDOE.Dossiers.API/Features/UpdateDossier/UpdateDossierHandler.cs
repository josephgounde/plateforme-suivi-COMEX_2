using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Dossiers.API.Mapping;
using PDOE.Infrastructure;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Dossiers.API.Features.UpdateDossier;

public class UpdateDossierHandler(PdoeDbContext db) : IRequestHandler<UpdateDossierCommand, DossierResponse>
{
    public async Task<DossierResponse> Handle(UpdateDossierCommand command, CancellationToken cancellationToken)
    {
        var dossier = await db.Dossiers
            .Include(d => d.EtapesWorkflow)
            .FirstOrDefaultAsync(d => d.DossierId == command.DossierId, cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        var request = command.Request;
        if (request.TypeOperation is not null) dossier.TypeOperation = request.TypeOperation.ToString()!;
        if (request.Montant is not null) dossier.Montant = request.Montant.Value;
        if (request.Devise is not null) dossier.Devise = request.Devise;
        if (request.PaysBeneficiaire is not null) dossier.PaysBeneficiaire = request.PaysBeneficiaire;
        if (request.Motif is not null) dossier.Motif = request.Motif;
        if (request.MatriculeClient is not null) dossier.MatriculeClient = request.MatriculeClient;
        if (request.NomBeneficiaire is not null) dossier.NomBeneficiaire = request.NomBeneficiaire;
        if (request.NatureTransaction is not null) dossier.NatureTransaction = request.NatureTransaction;
        if (request.ReferenceDomiciliation is not null) dossier.ReferenceDomiciliation = request.ReferenceDomiciliation;
        if (request.CodeStatistiqueOperateur is not null) dossier.CodeStatistiqueOperateur = request.CodeStatistiqueOperateur;
        if (request.NifClient is not null) dossier.NifClient = request.NifClient;
        if (request.AdressePostaleClient is not null) dossier.AdressePostaleClient = request.AdressePostaleClient;
        if (request.AdresseGeographiqueClient is not null) dossier.AdresseGeographiqueClient = request.AdresseGeographiqueClient;
        if (request.TelephoneClient is not null) dossier.TelephoneClient = request.TelephoneClient;
        if (request.CodeBanque is not null) dossier.CodeBanque = request.CodeBanque;
        if (request.QualiteResidence is not null) dossier.QualiteResidence = request.QualiteResidence.ToString();
        if (request.DateOuvertureCompte is not null) dossier.DateOuvertureCompte = DateOnly.FromDateTime(request.DateOuvertureCompte.Value.UtcDateTime);
        if (request.AnneeExerciceCompte is not null) dossier.AnneeExerciceCompte = request.AnneeExerciceCompte;
        if (request.TypeCompteDebite is not null) dossier.TypeCompteDebite = request.TypeCompteDebite.ToString()!;
        if (request.CodeSwiftIndicatif is not null) dossier.CodeSwiftIndicatif = request.CodeSwiftIndicatif;
        if (request.BanqueCorrespondanteIndicative is not null) dossier.BanqueCorrespondanteIndicative = request.BanqueCorrespondanteIndicative;
        if (request.DateConfirmationClient is not null) dossier.DateConfirmationClient = request.DateConfirmationClient.Value.UtcDateTime;
        if (request.SoldeCompteVerifie is not null) dossier.SoldeCompteVerifie = request.SoldeCompteVerifie.Value;
        dossier.UpdatedAt = DateTime.UtcNow;
        dossier.UpdatedBy = CurrentUser.Login;

        await db.SaveChangesAsync(cancellationToken);

        return dossier.ToResponse();
    }
}
