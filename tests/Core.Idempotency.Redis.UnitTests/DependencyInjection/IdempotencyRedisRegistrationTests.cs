using Core.Idempotency.Abstractions;
using Core.Idempotency.DependencyInjection;
using Core.Idempotency.Redis.Builders;
using Core.Idempotency.Redis.DependencyInjection;
using Core.Idempotency.Redis.Storage;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Core.Idempotency.Redis.UnitTests.DependencyInjection;

public class IdempotencyRedisRegistrationTests
{
    [Fact]
    public void AddCoreIdempotencyRedis_Should_Register_ProviderServices()
    {
        // Arrange
        var services = CreateServices();

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
        var services = CreateServices();

        CreateServicesExpectingInvalidOperationException(
            services,
            null!,
            RedisMessages.RedisConfigurationRequired);
    }

    [Fact]
    public void AddCoreIdempotencyRedis_Should_Require_CoreIdempotency_Registration()
    {
        var services = new ServiceCollection();

        CreateServicesExpectingInvalidOperationException(
            services,
            conf => conf.EndPoints.Add("localhost:6379"),
            RedisMessages.IdempotencyRegistrationRequired);
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();

        services.AddCoreIdempotency(options =>
        {
            options.Enabled = true;
            options.InstanceName = "TestInstance";
        });

        return services;
    }

    private static void CreateServicesExpectingInvalidOperationException(
        ServiceCollection services,
        Action<ConfigurationOptions>? configurationOptions,
        string message)
    {
        var action = () => services.AddCoreIdempotencyRedis(options =>
        {
            options.Configuration = configurationOptions;
        });

        action.Should()
              .Throw<InvalidOperationException>()
              .WithMessage(message);
    }
}
