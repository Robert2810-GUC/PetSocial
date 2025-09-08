using Application.Common.Interfaces;
using Infrastructure.Services;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Application services
            services.AddSingleton<IImageService, ImageService>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();
            services.AddHttpClient<IGoogleRatingService, GoogleRatingService>();
            services.AddScoped<ISaveChangesInterceptor, LookupCacheInvalidationInterceptor>();

            // Redis or Memory Cache
            if (configuration.GetValue<bool>("Redis:UseRedis"))
            {
                var connectionString = configuration["Redis:ConnectionString"];

                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    // Parse full connection string with options
                    var options = ConfigurationOptions.Parse(connectionString, true);

                    // Recommended settings for cloud Redis
                    options.AbortOnConnectFail = false; 
                    options.ConnectRetry = 3;          
                    options.ConnectTimeout = 10000;   
                    options.SyncTimeout = 10000;       

                    services.AddStackExchangeRedisCache(opt =>
                    {
                        opt.ConfigurationOptions = options;
                    });

                    services.AddSingleton<ICacheService, RedisCacheService>();
                }
            }
            else
            {
                // Fallback to in-memory cache
                services.AddMemoryCache();
                services.AddSingleton<ICacheService, MemoryCacheService>();
            }

            return services;
        }
    }
}
