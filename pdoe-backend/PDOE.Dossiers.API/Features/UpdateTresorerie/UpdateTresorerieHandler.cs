using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Dossiers.API.Mapping;
using PDOE.Infrastructure;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Dossiers.API.Features.UpdateTresorerie;

public class UpdateTresorerieHandler(PdoeDbContext db) : IRequestHandler<UpdateTresorerieCommand, DossierResponse>
{
    public async Task<DossierResponse> Handle(UpdateTresorerieCommand command, CancellationToken cancellationToken)
    {
        var dossier = await db.Dossiers
            .Include(d => d.EtapesWorkflow)
            .FirstOrDefaultAsync(d => d.DossierId == command.DossierId, cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        var request = command.Request;

        if (request.TauxChange is not null) dossier.TauxChange = request.TauxChange;
        if (request.DeviseCotation is not null) dossier.DeviseCotation = request.DeviseCotation;
        if (request.CorrespondantDesigne is not null) dossier.CorrespondantDesigne = request.CorrespondantDesigne;
        if (request.BicCorrespondant is not null) dossier.BicCorrespondant = request.BicCorrespondant;
        // .Date (pas .UtcDateTime) : cf. commentaire équivalent dans CreerPaiementHandler.
        if (request.DateDebit is not null) dossier.DateDebit = DateOnly.FromDateTime(request.DateDebit.Value.Date);
        if (request.Couverture is not null) dossier.Couverture = request.Couverture;
        if (request.DisponibiliteFonds is not null) dossier.DisponibiliteFonds = request.DisponibiliteFonds.Value;

        dossier.UpdatedAt = DateTime.UtcNow;
        dossier.UpdatedBy = CurrentUser.Login;

        await db.SaveChangesAsync(cancellationToken);

        return dossier.ToResponse();
    }
}
