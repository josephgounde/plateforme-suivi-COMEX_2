using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Execution.API.Features.GetExecution;

public record GetExecutionQuery(int DossierId) : IRequest<ExecutionDetailResponse>;
