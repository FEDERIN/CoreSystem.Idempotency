using Core.Idempotency.Abstractions;
using Core.Idempotency.DependencyInjection;
using Core.Idempotency.Redis.Builders;
using Core.Idempotency.Redis.DependencyInjection;
using Core.Idempotency.Redis.Storage;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Idempotency.Redis.UnitTests.DependencyInjection;

public class IdempotencyRedisRegistrationTests
{
    [Fact]
    public void AddCoreIdempotencyRedis_Should_Register_ProviderServices()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddCoreIdempotency(options =>
        {
            options.Enabled = true;
            options.InstanceName = "TestInstance";
        });

        // Act
        services.AddCoreIdempotencyRedis(options =>
        {
            options.Configuration = conf =>
            {
                conf.EndPoints.Add("localhost:6379");
            };
        });

        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetRequiredService<IIdempotencyStorage>()
            .Should()
            .BeOfType<RedisIdempotencyStorage>();

        provider.GetRequiredService<IKeyBuilder>()
            .Should()
            .BeOfType<RedisKeyBuilder>();
    }

    [Fact]
    public void AddCoreIdempotencyRedis_Should_Throw_When_Configuration_Is_Null()
    {
        // Arrange
        var services = new ServiceCollection();
        // Act

        services.AddCoreIdempotency(options =>
        {
            options.Enabled = true;
            options.InstanceName = "TestInstance";
        });

        var action = () => services.AddCoreIdempotencyRedis(options =>
        {
            options.Configuration = null;
        });

        // Assert
        action.Should()
              .Throw<InvalidOperationException>()
              .WithMessage(RedisMessages.RedisConfigurationRequired);
    }


    [Fact]
    public void AddCoreIdempotencyRedis_Should_Require_CoreIdempotency_Registration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var action = () => services.AddCoreIdempotencyRedis(options =>
        {
            options.Configuration = conf =>
            {
                conf.EndPoints.Add("localhost:6379");
            };
        });

        // Assert
        action.Should()
              .Throw<InvalidOperationException>()
              .WithMessage(RedisMessages.IdempotencyRegistrationRequired);
    }
}
