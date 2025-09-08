using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Lookups.Queries;

public record GetBreedsQuery(long PetTypeId) : IRequest<ApiResponse<List<BreedDto>>>;

public record BreedDto(long Id, string Name);

public class GetBreedsQueryHandler : IRequestHandler<GetBreedsQuery, ApiResponse<List<BreedDto>>>
{
    private readonly ApplicationDbContext _db;
    private readonly ICacheService _cache;

    public GetBreedsQueryHandler(ApplicationDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<ApiResponse<List<BreedDto>>> Handle(GetBreedsQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"breeds-{request.PetTypeId}";
        var cached = await _cache.GetAsync<List<BreedDto>>(cacheKey);
        if (cached is not null)
            return ApiResponse<List<BreedDto>>.Success(cached);

        var breeds = await _db.PetBreeds
            .Where(b => b.PetTypeId == request.PetTypeId)
            .OrderBy(b => b.SortOrder)
            .Select(b => new BreedDto(b.Id, b.Name))
            .ToListAsync(cancellationToken);

        await _cache.SetAsync(cacheKey, breeds, TimeSpan.FromHours(1));
        return ApiResponse<List<BreedDto>>.Success(breeds);
    }
}

