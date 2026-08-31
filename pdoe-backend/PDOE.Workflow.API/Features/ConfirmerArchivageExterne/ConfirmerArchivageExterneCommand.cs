using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Workflow.API.Features.ConfirmerArchivageExterne;

public record ConfirmerArchivageExterneCommand(int DossierId) : IRequest<ConfirmationArchivageResponse>;
