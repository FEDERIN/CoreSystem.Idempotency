using Core.Idempotency.Abstractions;
using Core.Idempotency.Options;
using Core.Idempotency.Redis.Builders;
using Core.Idempotency.Redis.Options;
using Core.Idempotency.Redis.Storage;
using Core.Redis.Connection;
using Core.Redis.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Core.Idempotency.Redis.DependencyInjection;

public static class IdempotencyRedisRegistration
{
    public static IServiceCollection AddCoreIdempotencyRedis(
        this IServiceCollection services,
        Action<RedisOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        EnsureIdempotencyRegistered(services);

        var options = BuildOptions(configure);

        services.AddCoreRedis();

        services.AddSingleton(options);

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            var factory = sp.GetRequiredService<IRedisConnectionFactory>();

            return factory.Create(options.Configuration!);
        });

        services.AddSingleton<IKeyBuilder>(sp =>
        {
            var options = sp.GetRequiredService<IdempotencyOptions>();

            var prefix = string.IsNullOrWhiteSpace(options.InstanceName)
                ? string.Empty
                : $"{options.InstanceName}:";

            return new RedisKeyBuilder(prefix);
        });

        services.AddSingleton<IIdempotencyStorage, RedisIdempotencyStorage>();

        return services;
    }

    private static RedisOptions BuildOptions(
        Action<RedisOptions> configure)
    {
        var options = new RedisOptions();

        configure(options);

        if (options.Configuration is null)
        {
            throw new InvalidOperationException(
                RedisMessages.RedisConfigurationRequired);
        }

        return options;
    }

    private static void EnsureIdempotencyRegistered(
        IServiceCollection services)
    {
        if (services.All(
                d => d.ServiceType != typeof(IdempotencyOptions)))
        {
            throw new InvalidOperationException(
                RedisMessages.IdempotencyRegistrationRequired);
        }
    }
}