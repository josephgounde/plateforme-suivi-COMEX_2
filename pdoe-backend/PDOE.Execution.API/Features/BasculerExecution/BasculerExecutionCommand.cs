using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Execution.API.Features.BasculerExecution;

public record BasculerExecutionCommand(int DossierId) : IRequest<DossierResponse>;
