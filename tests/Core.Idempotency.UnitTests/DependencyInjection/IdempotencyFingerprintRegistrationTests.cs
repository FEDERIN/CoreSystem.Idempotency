using Core.Idempotency.DependencyInjection;
using Core.Idempotency.Fingerprint;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Idempotency.UnitTests.DependencyInjection;

public class IdempotencyFingerprintRegistrationTests
{
    [Fact]
    public void AddCoreIdempotency_Should_Register_FingerprintServices_When_Enabled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCoreIdempotency(options =>
        {
            options.Enabled = true;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetRequiredService<IRequestFingerprintBuilder>()
            .Should()
            .BeOfType<RequestFingerprintBuilder>();

        provider.GetRequiredService<IRequestHasher>()
            .Should()
            .BeOfType<Sha256RequestHasher>();

        provider.GetRequiredService<IRequestFingerprintProvider>()
            .Should()
            .BeOfType<DefaultRequestFingerprintProvider>();
    }

    [Fact]
    public void AddCoreIdempotency_Should_Not_Register_FingerprintServices_When_Disabled()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddCoreIdempotency(options =>
        {
            options.Enabled = false;
        });

        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IRequestFingerprintBuilder>()
            .Should()
            .BeNull();

        provider.GetService<IRequestHasher>()
            .Should()
            .BeNull();

        provider.GetService<IRequestFingerprintProvider>()
            .Should()
            .BeNull();
    }
}