using Application.Common.Interfaces;
using Infrastructure.Services;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IImageService, ImageService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddHttpClient<IGoogleRatingService, GoogleRatingService>();
        services.AddScoped<ISaveChangesInterceptor, LookupCacheInvalidationInterceptor>();

        if (configuration.GetValue<bool>("Redis:UseRedis"))
        {
            var connection = configuration["Redis:ConnectionString"];

            if (!string.IsNullOrWhiteSpace(connection) &&
                (connection.StartsWith("redis://") || connection.StartsWith("rediss://")))
            {
                var uri = new Uri(connection);
                var userInfo = uri.UserInfo.Split(':', 2);
                var password = userInfo.Length > 1 ? userInfo[1] : string.Empty;
                var useSsl = uri.Scheme == "rediss";
                connection = $"{uri.Host}:{uri.Port},password={password},ssl={useSsl},abortConnect=False";
            }

            services.AddStackExchangeRedisCache(opt => opt.Configuration = connection);
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
