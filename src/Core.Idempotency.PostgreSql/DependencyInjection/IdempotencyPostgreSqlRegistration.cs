using Core.Idempotency.Abstractions;
using Core.Idempotency.Options;
using Core.Idempotency.PostgreSql.Options;
using Core.Idempotency.PostgreSql.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Idempotency.PostgreSql.DependencyInjection;

/// <summary>
/// Registration entry points for the PostgreSQL storage provider.
/// </summary>
public static class IdempotencyPostgreSqlRegistration
{
    /// <summary>
    /// Registers PostgreSQL as the idempotency storage provider.
    /// </summary>
    /// <remarks>
    /// <c>AddCoreIdempotency</c> must be called first. Assign
    /// <see cref="PostgreSqlOptions.ConnectionString"/>.
    /// <para>
    /// This provider does not create its schema. Apply the idempotent script shipped
    /// at <c>scripts/postgres.sql</c> to the target database before the first request.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configure">Callback used to configure the connection string.</param>
    /// <returns>The same service collection, to allow chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>AddCoreIdempotency</c> has not been called, or when
    /// <see cref="PostgreSqlOptions.ConnectionString"/> was left null or empty.
    /// </exception>
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