using Core.Idempotency.Abstractions;
using Core.Idempotency.KeyResolution;
using Core.Idempotency.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Idempotency.DependencyInjection;

internal static class ServiceRegistration
{
    public static IServiceCollection AddIdempotencyServices(
        this IServiceCollection services)
    {
        services.TryAddSingleton<IIdempotencyKeyResolver, HeaderIdempotencyKeyResolver>();
        services.AddSingleton<IIdempotencyService, IdempotencyService>();

        return services;
    }
}