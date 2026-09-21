using Core.Idempotency.Fingerprint;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Idempotency.DependencyInjection;

internal static class FingerprintRegistration
{
    public static IServiceCollection AddFingerprint(
        this IServiceCollection services)
    {
        services.AddSingleton<IRequestFingerprintProvider, DefaultRequestFingerprintProvider>();
        services.AddSingleton<IRequestFingerprintBuilder, RequestFingerprintBuilder>();
        services.AddSingleton<IRequestHasher, Sha256RequestHasher>();

        return services;
    }
}