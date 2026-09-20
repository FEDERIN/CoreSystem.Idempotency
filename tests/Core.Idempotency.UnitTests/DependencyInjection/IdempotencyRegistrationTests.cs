using Core.Idempotency.Abstractions;
using Core.Idempotency.DependencyInjection;
using Core.Idempotency.Options;
using Core.Idempotency.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Core.Idempotency.UnitTests.DependencyInjection;

public class IdempotencyRegistrationTests
{
    [Fact]
    public void AddCoreIdempotency_Should_Register_Required_Services_When_Enabled()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddCoreIdempotency(options =>
        {
            options.Enabled = true;
        });

        // Satisfy IdempotencyService dependencies
        services.AddSingleton(Mock.Of<IIdempotencyStorage>());

        // Act
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetRequiredService<IdempotencyOptions>();

        provider.GetRequiredService<IIdempotencyKeyResolver>();

        provider.GetRequiredService<IIdempotencyService>()
            .Should()
            .BeOfType<IdempotencyService>();
    }

    [Fact]
    public void AddCoreIdempotency_Should_Register_Configured_Options()
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
        var options = provider.GetRequiredService<IdempotencyOptions>();

        options.Enabled.Should().BeFalse();
    }

    [Fact]
    public void AddCoreIdempotency_Should_Return_ServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddCoreIdempotency(options =>
        {
            options.Enabled = false;
        });

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddCoreIdempotency_Should_Not_Register_CoreServices_When_Disabled()
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
        provider.GetService<IIdempotencyService>()
            .Should()
            .BeNull();

        provider.GetService<IIdempotencyKeyResolver>()
            .Should()
            .BeNull();
    }
}