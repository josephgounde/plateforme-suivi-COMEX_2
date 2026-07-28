using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Dossiers.API.Features.ListDossiers;

public record ListDossiersQuery(
    StatutDossier? Statut,
    TypeOperation? TypeOperation,
    string? NumCompte,
    DateOnly? DateDebutCreation,
    DateOnly? DateFinCreation,
    int Page,
    int PageSize) : IRequest<DossierListResponse>;
