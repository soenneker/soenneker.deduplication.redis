using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.Deduplication.Redis.Abstract;
using Soenneker.Redis.Util.Registrars;

namespace Soenneker.Deduplication.Redis.Registrars;

/// <summary>
/// Distributed duplicate suppression backed by Redis.
/// </summary>
public static class RedisDedupeRegistrar
{
    /// <summary>
    /// Adds <see cref="IRedisDedupe"/> as a singleton service. <para/>
    /// </summary>
    public static IServiceCollection AddRedisDedupeAsSingleton(this IServiceCollection services)
    {
        services.AddRedisUtilAsSingleton().TryAddSingleton<IRedisDedupe, RedisDedupe>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="IRedisDedupe"/> as a scoped service. <para/>
    /// </summary>
    public static IServiceCollection AddRedisDedupeAsScoped(this IServiceCollection services)
    {
        services.AddRedisUtilAsScoped().TryAddScoped<IRedisDedupe, RedisDedupe>();

        return services;
    }
}