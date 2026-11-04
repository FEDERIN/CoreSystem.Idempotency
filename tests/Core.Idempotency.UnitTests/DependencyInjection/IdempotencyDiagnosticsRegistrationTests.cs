using Core.Idempotency.DependencyInjection;
using Core.Idempotency.Diagnostics;
using Core.Observability.Abstractions;
using FluentAssertions;
using Core.Idempotency.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Idempotency.UnitTests.DependencyInjection;

public class IdempotencyDiagnosticsRegistrationTests
{
    [Fact]
    public void AddCoreIdempotency_Should_Register_IdempotencyMetrics_When_Enabled()
    {
        var provider = ServiceProviderFactory.Create(true);

        provider.GetRequiredService<IdempotencyMetrics>();
    }

    [Fact]
    public void AddCoreIdempotency_Should_Register_ObservabilityContributor_When_Enabled()
    {
        var provider = ServiceProviderFactory.Create(true);

        provider.GetServices<IObservabilityContributor>()
                .Should()
                .ContainSingle(x => x is IdempotencyObservabilityContributor);
    }

    [Fact]
    public void AddCoreIdempotency_Should_Not_Register_IdempotencyMetrics_When_Disabled()
    {
        var provider = ServiceProviderFactory.Create(false);

        provider.GetService<IdempotencyMetrics>()
                .Should()
                .BeNull();
    }

    [Fact]
    public void AddCoreIdempotency_Should_Not_Register_ObservabilityContributor_When_Disabled()
    {
        var provider = ServiceProviderFactory.Create(false);

        provider.GetServices<IObservabilityContributor>()
                .Should()
                .BeEmpty();
    }


}