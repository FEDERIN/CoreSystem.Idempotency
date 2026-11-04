using Core.Idempotency.DependencyInjection;
using Core.Idempotency.Fingerprint;
using FluentAssertions;
using Core.Idempotency.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Idempotency.UnitTests.DependencyInjection;

public class IdempotencyFingerprintRegistrationTests
{
    [Fact]
    public void AddCoreIdempotency_Should_Register_FingerprintServices_When_Enabled()
    {
        var provider = ServiceProviderFactory.Create(true);

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
        var provider = ServiceProviderFactory.Create(false);

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