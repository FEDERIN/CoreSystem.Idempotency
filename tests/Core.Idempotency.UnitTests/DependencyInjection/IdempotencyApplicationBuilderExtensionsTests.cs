using Core.Idempotency.DependencyInjection;
using Core.Idempotency.Options;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Idempotency.UnitTests.DependencyInjection;

public class IdempotencyApplicationBuilderExtensionsTests
{
    [Fact]
    public void UseCoreIdempotency_Should_Throw_When_ApplicationBuilder_Is_Null()
    {
        // Arrange
        IApplicationBuilder app = null!;

        // Act
        var action = () => app.UseCoreIdempotency();

        // Assert
        action.Should()
              .Throw<ArgumentNullException>()
              .WithParameterName("app");
    }

    [Fact]
    public void UseCoreIdempotency_Should_Throw_When_CoreIdempotency_Is_Not_Registered()
    {
        // Arrange
        var services = new ServiceCollection();

        var provider = services.BuildServiceProvider();

        var app = new ApplicationBuilder(provider);

        // Act
        var action = () => app.UseCoreIdempotency();

        // Assert
        action.Should()
              .Throw<InvalidOperationException>()
              .WithMessage(IdempotencyMessages.MissingRegistration);
    }

    [Fact]
    public void UseCoreIdempotency_Should_Return_ApplicationBuilder_When_Disabled()
    {
        var services = new ServiceCollection();

        services.AddSingleton(new IdempotencyOptions
        {
            Enabled = false
        });

        var provider = services.BuildServiceProvider();

        var app = new ApplicationBuilder(provider);

        var result = app.UseCoreIdempotency();

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseCoreIdempotency_Should_Throw_When_IdempotencyService_Is_Not_Registered()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddSingleton(new IdempotencyOptions
        {
            Enabled = true
        });

        var provider = services.BuildServiceProvider();

        var app = new ApplicationBuilder(provider);

        // Act
        var action = () => app.UseCoreIdempotency();

        // Assert
        action.Should()
              .Throw<InvalidOperationException>()
              .WithMessage(IdempotencyMessages.MissingRegistration);
    }
}