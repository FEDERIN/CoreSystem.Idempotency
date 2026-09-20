using Core.Idempotency.Diagnostics;
using Core.Observability.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Idempotency.DependencyInjection;

internal static class DiagnosticsRegistration
{
    public static IServiceCollection AddIdempotencyDiagnostics(
        this IServiceCollection services)
    {
        services.AddMetrics();

        services.AddSingleton<IdempotencyMetrics>();

        services.AddSingleton<IObservabilityContributor>(
            new IdempotencyObservabilityContributor());

        return services;
    }
}
