using Core.Http.DependencyInjection;
using Core.Idempotency.Abstractions;
using Core.Idempotency.Middleware;
using Core.Idempotency.Options;
using Core.Serialization;
using Core.Serialization.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Idempotency.DependencyInjection;

public static class IdempotencyRegistration
{
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