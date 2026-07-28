using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Apurement.API.Features.ValiderChecklist;

public record ValiderChecklistCommand(int DossierId, ChecklistRequest Request) : IRequest<DossierResponse>;
