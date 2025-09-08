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
                    ConfigurationOptions options;

                    // Support both standard and URI formatted connection strings
                    if (connectionString.StartsWith("redis://", StringComparison.OrdinalIgnoreCase) ||
                        connectionString.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
                    {
                        var uri = new Uri(connectionString);
                        options = new ConfigurationOptions
                        {
                            Ssl = uri.Scheme == "rediss"
                        };

                        // Add host and port
                        options.EndPoints.Add(uri.Host, uri.Port);

                        // Extract user/password if present
                        if (!string.IsNullOrEmpty(uri.UserInfo))
                        {
                            var parts = uri.UserInfo.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length == 2)
                            {
                                options.User = Uri.UnescapeDataString(parts[0]);
                                options.Password = Uri.UnescapeDataString(parts[1]);
                            }
                            else
                            {
                                options.Password = Uri.UnescapeDataString(parts[0]);
                            }
                        }

                        // Parse query string parameters such as ssl or abortConnect
                        var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var p in query)
                        {
                            var kv = p.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
                            if (kv.Length != 2) continue;
                            var key = kv[0].ToLowerInvariant();
                            var value = kv[1];
                            if (key == "ssl" && bool.TryParse(value, out var ssl))
                            {
                                options.Ssl = ssl;
                            }
                            else if (key == "abortconnect" && bool.TryParse(value, out var abort))
                            {
                                options.AbortOnConnectFail = abort;
                            }
                        }
                    }
                    else
                    {
                        // Parse full connection string with options
                        options = ConfigurationOptions.Parse(connectionString, true);
                    }

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
