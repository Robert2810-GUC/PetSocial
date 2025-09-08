using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Lookups.Queries;

public record GetColorsQuery : IRequest<ApiResponse<List<ColorDto>>>;

public record ColorDto(long Id, string Name);

public class GetColorsQueryHandler : IRequestHandler<GetColorsQuery, ApiResponse<List<ColorDto>>>
{
    private readonly ApplicationDbContext _db;
    private readonly ICacheService _cache;

    public GetColorsQueryHandler(ApplicationDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<ApiResponse<List<ColorDto>>> Handle(GetColorsQuery request, CancellationToken cancellationToken)
    {
        const string cacheKey = "colors";
        var cached = await _cache.GetAsync<List<ColorDto>>(cacheKey);
        if (cached is not null)
            return ApiResponse<List<ColorDto>>.Success(cached);

        var colors = await _db.PetColors
            .OrderBy(c => c.SortOrder)
            .Select(c => new ColorDto(c.Id, c.Name))
            .ToListAsync(cancellationToken);

        await _cache.SetAsync(cacheKey, colors, TimeSpan.FromHours(1));
        return ApiResponse<List<ColorDto>>.Success(colors);
    }
}

