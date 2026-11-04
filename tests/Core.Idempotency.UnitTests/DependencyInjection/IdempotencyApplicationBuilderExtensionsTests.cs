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
        var app = CreateApp(null);

        var action = () => app.UseCoreIdempotency();

        action.Should()
              .Throw<InvalidOperationException>()
              .WithMessage(IdempotencyMessages.MissingRegistration);
    }

    [Fact]
    public void UseCoreIdempotency_Should_Return_ApplicationBuilder_When_Disabled()
    {
        var app = CreateApp(new IdempotencyOptions { Enabled = false });

        var result = app.UseCoreIdempotency();

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseCoreIdempotency_Should_Throw_When_IdempotencyService_Is_Not_Registered()
    {
        var app = CreateApp(new IdempotencyOptions { Enabled = true });

        var action = () => app.UseCoreIdempotency();

        action.Should()
              .Throw<InvalidOperationException>()
              .WithMessage(IdempotencyMessages.MissingRegistration);
    }

    private static ApplicationBuilder CreateApp(IdempotencyOptions? options)
    {
        var services = new ServiceCollection();

        if(options is not null)
            services.AddSingleton(options);
        
        return new ApplicationBuilder(services.BuildServiceProvider());
    }
}