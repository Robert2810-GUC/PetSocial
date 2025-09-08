using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Lookups.Queries;

public record GetPetFoodsQuery : IRequest<ApiResponse<List<PetFoodDto>>>;

public record PetFoodDto(long Id, string Name);

public class GetPetFoodsQueryHandler : IRequestHandler<GetPetFoodsQuery, ApiResponse<List<PetFoodDto>>>
{
    private readonly ApplicationDbContext _db;
    private readonly ICacheService _cache;

    public GetPetFoodsQueryHandler(ApplicationDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<ApiResponse<List<PetFoodDto>>> Handle(GetPetFoodsQuery request, CancellationToken cancellationToken)
    {
        const string cacheKey = "foods";
        var cached = await _cache.GetAsync<List<PetFoodDto>>(cacheKey);
        if (cached is not null)
            return ApiResponse<List<PetFoodDto>>.Success(cached);

        var foods = await _db.PetFoods
            .OrderBy(c => c.SortOrder)
            .Select(c => new PetFoodDto(c.Id, c.Name))
            .ToListAsync(cancellationToken);

        await _cache.SetAsync(cacheKey, foods, TimeSpan.FromHours(1));
        return ApiResponse<List<PetFoodDto>>.Success(foods);
    }
}

