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

/// <summary>
/// Registration entry points for the Redis storage provider.
/// </summary>
public static class IdempotencyRedisRegistration
{
    /// <summary>
    /// Registers Redis as the idempotency storage provider.
    /// </summary>
    /// <remarks>
    /// <c>AddCoreIdempotency</c> must be called first. Assign
    /// <see cref="RedisOptions.Configuration"/>; it is an
    /// <c>Action&lt;ConfigurationOptions&gt;</c> and cannot be populated with
    /// <c>Bind</c>, because configuration binding cannot construct a delegate.
    /// </remarks>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configure">Callback used to configure the Redis connection.</param>
    /// <returns>The same service collection, to allow chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>AddCoreIdempotency</c> has not been called, or when
    /// <see cref="RedisOptions.Configuration"/> was left unassigned.
    /// </exception>
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