using Core.Idempotency.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Idempotency.UnitTests.Helpers;

public static class ServiceProviderFactory
{
    public static ServiceProvider Create(bool enabled, Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddCoreIdempotency(options => options.Enabled = enabled);
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }
}