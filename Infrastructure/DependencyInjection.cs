using Application.Common.Interfaces;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IImageService, ImageService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddHttpClient<IGoogleRatingService, GoogleRatingService>();

        if (configuration.GetValue<bool>("Redis:UseRedis"))
        {
            services.AddStackExchangeRedisCache(opt =>
                opt.Configuration = configuration["Redis:ConnectionString"]);
            services.AddSingleton<ICacheService, RedisCacheService>();
        }
        else
        {
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }

        return services;
    }
}
