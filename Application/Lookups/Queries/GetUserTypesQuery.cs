using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace Application.Lookups.Queries;

public record GetUserTypesQuery : IRequest<ApiResponse<List<UserTypeDto>>>;

public record UserTypeDto(long Id, string Name, string? ImagePath, string? Description);

public class GetUserTypesQueryHandler : IRequestHandler<GetUserTypesQuery, ApiResponse<List<UserTypeDto>>>
{
    private readonly ApplicationDbContext _db;
    private readonly ICacheService _cache;

    public GetUserTypesQueryHandler(ApplicationDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<ApiResponse<List<UserTypeDto>>> Handle(GetUserTypesQuery request, CancellationToken cancellationToken)
    {
        const string cacheKey = "user-types";
        var cached = await _cache.GetAsync<List<UserTypeDto>>(cacheKey);
        if (cached is not null)
            return ApiResponse<List<UserTypeDto>>.Success(cached);

        var types = await _db.UserTypes
            .Select(c => new UserTypeDto(c.Id, c.Name, c.ImagePath, c.Description))
            .ToListAsync(cancellationToken);

        await _cache.SetAsync(cacheKey, types, TimeSpan.FromHours(1));
        return ApiResponse<List<UserTypeDto>>.Success(types);
    }
}

