using Core.Idempotency.Abstractions;
using Core.Idempotency.Options;
using Core.Idempotency.PostgreSql.Options;
using Core.Idempotency.PostgreSql.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Idempotency.PostgreSql.DependencyInjection;

public static class IdempotencyPostgreSqlRegistration
{
    public static IServiceCollection AddCoreIdempotencyPostgreSql(
        this IServiceCollection services,
        Action<PostgreSqlOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        EnsureIdempotencyRegistered(services);

        var options = BuildOptions(configure);

        services
            .AddSingleton(options)
            .AddSingleton<IIdempotencyStorage, PostgreSqlIdempotencyStorage>();

        return services;
    }

    private static PostgreSqlOptions BuildOptions(
        Action<PostgreSqlOptions> configure)
    {
        var options = new PostgreSqlOptions();

        configure(options);

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            throw new InvalidOperationException(
                PostgreSqlMessages.PostgreSqlConnectionStringRequired);
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
                PostgreSqlMessages.IdempotencyRegistrationRequired);
        }
    }
}