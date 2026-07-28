using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Execution.API.Features.DeclarerExecution;

public record DeclarerExecutionCommand(int DossierId, DeclarerExecutionRequest Request) : IRequest<ExecutionDeclarationResponse>;
