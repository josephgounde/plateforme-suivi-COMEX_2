using MediatR;
using PDOE.Api.Contracts;

namespace PDOE.Reporting.API.Features.ListExportsReglementaires;

public record ListExportsReglementairesQuery(
    CategorieExport? Categorie,
    TypeExport? TypeExport,
    DateOnly? DateDebut,
    DateOnly? DateFin,
    int Page,
    int PageSize) : IRequest<ExportReglementaireListResponse>;
