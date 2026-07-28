using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Dossiers.API.Features.NotifierClient;

public record NotifierClientCommand(int DossierId, NotifierClientRequest Request) : IRequest<NotifierClientResponse>;
