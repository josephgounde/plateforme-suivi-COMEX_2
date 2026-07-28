using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;
using PDOE.Shared.Kernel.Common;

namespace PDOE.Execution.API.Features.GetExecution;

public class GetExecutionHandler(PdoeDbContext db) : IRequestHandler<GetExecutionQuery, ExecutionDetailResponse>
{
    public async Task<ExecutionDetailResponse> Handle(GetExecutionQuery query, CancellationToken cancellationToken)
    {
        var dossier = await db.Dossiers.FirstOrDefaultAsync(d => d.DossierId == query.DossierId, cancellationToken);

        if (dossier is null)
            throw new DomainException(404, ErrorResponseCode.DOSSIER_INTROUVABLE, "Dossier introuvable.");

        return new ExecutionDetailResponse
        {
            DossierId = dossier.DossierId,
            ReferenceInterne = dossier.ReferenceInterne,
            Montant = (double)dossier.Montant,
            Devise = dossier.Devise,
            TauxChange = (double?)dossier.TauxChange,
            CorrespondantDesigne = dossier.CorrespondantDesigne,
            BicCorrespondant = dossier.BicCorrespondant,
            DateDebit = dossier.DateDebit?.ToDateTime(TimeOnly.MinValue),
            ReferenceABS = dossier.ReferenceABS,
            ReferenceSWIFT = dossier.ReferenceSWIFT,
            NumeroAC = dossier.NumeroAC,
            CodeTRF = dossier.CodeTRF,
            DateExecution = dossier.DateExecution,
            StatutElectronique = Enum.Parse<StatutDossier>(dossier.StatutElectronique),
        };
    }
}
