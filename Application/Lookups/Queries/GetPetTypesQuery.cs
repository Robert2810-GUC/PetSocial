using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Lookups.Queries;

public record GetPetTypesQuery : IRequest<ApiResponse<List<PetTypeDto>>>;

public record PetTypeDto(long Id, string Name, string? ImagePath);

public class GetPetTypesQueryHandler : IRequestHandler<GetPetTypesQuery, ApiResponse<List<PetTypeDto>>>
{
    private readonly ApplicationDbContext _db;
    private readonly ICacheService _cache;

    public GetPetTypesQueryHandler(ApplicationDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<ApiResponse<List<PetTypeDto>>> Handle(GetPetTypesQuery request, CancellationToken cancellationToken)
    {
        const string cacheKey = "pet-types";
        var cached = await _cache.GetAsync<List<PetTypeDto>>(cacheKey);
        if (cached is not null)
            return ApiResponse<List<PetTypeDto>>.Success(cached);

        var types = await _db.PetTypes
            .OrderBy(t => t.SortOrder)
            .Select(t => new PetTypeDto(t.Id, t.Name, t.ImagePath))
            .ToListAsync(cancellationToken);

        await _cache.SetAsync(cacheKey, types, TimeSpan.FromHours(1));
        return ApiResponse<List<PetTypeDto>>.Success(types);
    }
}

