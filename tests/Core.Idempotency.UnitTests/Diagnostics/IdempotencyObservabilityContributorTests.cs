using Core.Idempotency.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;

namespace Core.Idempotency.UnitTests.Diagnostics;

public class IdempotencyObservabilityContributorTests
{
    [Fact]
    public void GetActivitySources_ShouldReturnCacheSource()
    {
        // Arrange
        var contributor = new IdempotencyObservabilityContributor();

        // Act
        var sources = contributor.GetActivitySources();

        // Assert
        sources.Should().ContainSingle();
        sources.Should().Contain(IdempotencyDiagnosticsConstants.MeterName);
    }

    [Fact]
    public void ConfigureObservability_ShouldRegisterOpenTelemetry()
    {
        // Arrange
        var services = new ServiceCollection();

        var contributor = new IdempotencyObservabilityContributor();

        // Act
        contributor.ConfigureObservability(
            services,
            new ConfigurationBuilder().Build());

        var provider = services.BuildServiceProvider();

        // Assert

        provider.GetService<MeterProvider>()
            .Should()
            .NotBeNull();
    }
}
