using Core.Http.DependencyInjection;
using Core.Idempotency.Abstractions;
using Core.Idempotency.Middleware;
using Core.Idempotency.Options;
using Core.Serialization;
using Core.Serialization.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Idempotency.DependencyInjection;

/// <summary>
/// Registration entry points for the CoreSystem.Idempotency middleware.
/// </summary>
public static class IdempotencyRegistration
{
    /// <summary>
    /// Registers idempotency options, fingerprinting, diagnostics and the core services.
    /// </summary>
    /// <remarks>
    /// A storage provider must also be registered, for example
    /// <c>AddCoreIdempotencyRedis</c> or <c>AddCoreIdempotencyPostgreSql</c>.
    /// When <see cref="IdempotencyOptions.Enabled"/> is <see langword="false"/> the
    /// call registers only the options and installs nothing else.
    /// </remarks>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configure">Callback used to populate the options.</param>
    /// <returns>The same service collection, to allow chaining.</returns>
    public static IServiceCollection AddCoreIdempotency(
        this IServiceCollection services,
        Action<IdempotencyOptions> configure)
    {
        var options = new IdempotencyOptions();

        configure(options);

        services
            .AddSingleton(options);

        if (!options.Enabled)
        {
            return services;
        }
        
        services.AddCoreSerialization(serialization =>
            {
                serialization.DefaultSerializer = SerializerType.Json;
            })
            .AddCoreHttp()
            .AddIdempotencyDiagnostics()
            .AddFingerprint();

        services.AddIdempotencyServices();

        return services;
    }

    /// <summary>
    /// Adds the idempotency middleware to the request pipeline.
    /// </summary>
    /// <remarks>
    /// Place this after routing and before the endpoints. When idempotency is
    /// disabled, or no storage provider has been registered, the pipeline is left
    /// unchanged; the latter throws, because it is a configuration error.
    /// </remarks>
    /// <param name="app">The application builder to configure.</param>
    /// <returns>The same application builder, to allow chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>AddCoreIdempotency</c> has not been called, or when no
    /// <c>IIdempotencyService</c> is registered because no storage provider was added.
    /// </exception>
    public static IApplicationBuilder UseCoreIdempotency(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var options = app.ApplicationServices.GetService<IdempotencyOptions>() 
            ?? throw new InvalidOperationException(
                IdempotencyMessages.MissingRegistration);

        if (!options.Enabled)
        {
            return app;
        }

        if (app.ApplicationServices.GetService<IIdempotencyService>() is null)
        {
            throw new InvalidOperationException(
                IdempotencyMessages.MissingRegistration);
        }

        return app.UseMiddleware<IdempotencyMiddleware>();
    }
}