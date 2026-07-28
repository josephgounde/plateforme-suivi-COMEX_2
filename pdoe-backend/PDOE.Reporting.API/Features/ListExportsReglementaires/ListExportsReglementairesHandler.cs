using MediatR;
using Microsoft.EntityFrameworkCore;
using PDOE.Api.Contracts;
using PDOE.Infrastructure;

namespace PDOE.Reporting.API.Features.ListExportsReglementaires;

public class ListExportsReglementairesHandler(PdoeDbContext db) : IRequestHandler<ListExportsReglementairesQuery, ExportReglementaireListResponse>
{
    public async Task<ExportReglementaireListResponse> Handle(ListExportsReglementairesQuery request, CancellationToken cancellationToken)
    {
        var query = db.ExportsReglementaires.AsQueryable();

        if (request.Categorie is { } categorie)
            query = query.Where(e => e.Categorie == categorie.ToString());

        if (request.TypeExport is { } typeExport)
            query = query.Where(e => e.TypeExport == typeExport.ToString());

        // Chevauchement de période, pas égalité exacte : un export DateDebut=01/07 DateFin=31/07 doit
        // remonter pour un filtre dateDebut=15/07 même si les bornes ne correspondent pas exactement.
        if (request.DateDebut is { } dateDebut)
            query = query.Where(e => e.DateFin >= dateDebut);

        if (request.DateFin is { } dateFin)
            query = query.Where(e => e.DateDebut <= dateFin);

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 100 ? 20 : request.PageSize;

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new ExportReglementaireListResponse
        {
            Items = items.Select(e => new ExportReglementaireResponse
            {
                ExportReglementaireId = e.ExportReglementaireId,
                Categorie = Enum.Parse<CategorieExport>(e.Categorie),
                TypeExport = Enum.Parse<TypeExport>(e.TypeExport),
                DateDebut = new DateTimeOffset(e.DateDebut.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                DateFin = new DateTimeOffset(e.DateFin.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero),
                NomFichier = e.NomFichier,
                HashSHA256 = e.HashSHA256,
                TailleFichier = e.TailleFichier,
                CreatedAt = e.CreatedAt,
                CreatedBy = e.CreatedBy,
            }).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }
}
