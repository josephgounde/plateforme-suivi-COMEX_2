using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.CBS.Integration.Features.ObtenirTauxChange;

public record ObtenirTauxChangeQuery(string? Devise, string? VersDevise) : IRequest<TauxChangeResult>;
