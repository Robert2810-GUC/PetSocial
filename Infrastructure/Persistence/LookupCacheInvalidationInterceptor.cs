using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Persistence;

public class LookupCacheInvalidationInterceptor : SaveChangesInterceptor
{
    private readonly ICacheService _cache;

    public LookupCacheInvalidationInterceptor(ICacheService cache)
    {
        _cache = cache;
    }

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            var changed = eventData.Context.ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                .Select(e => e.Entity)
                .ToList();

            if (changed.Any(e => e is PetType))
                await _cache.RemoveAsync("pet-types");
            if (changed.Any(e => e is PetFood))
                await _cache.RemoveAsync("foods");
            if (changed.Any(e => e is PetColor))
                await _cache.RemoveAsync("colors");
            if (changed.Any(e => e is UserType))
                await _cache.RemoveAsync("user-types");

            var breedIds = changed.OfType<PetBreed>().Select(b => b.PetTypeId).Distinct();
            foreach (var id in breedIds)
            {
                await _cache.RemoveAsync($"breeds-{id}");
            }
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
