using Core.Idempotency.Abstractions;
using Core.Idempotency.DependencyInjection;
using Core.Idempotency.PostgreSql.DependencyInjection;
using Core.Idempotency.PostgreSql.Storage;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Idempotency.PostgreSql.UnitTests.DependencyInjection;


public class IdempotencyPostgreSqlRegistrationTests
{
    [Fact]
    public void AddCoreIdempotencyPostgreSql_Should_Register_ProviderServices()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddCoreIdempotency(options =>
        {
            options.Enabled = true;
            options.InstanceName = "TestInstance";
        });

        // Act
        services.AddCoreIdempotencyPostgreSql(options =>
        {
            options.ConnectionString =
                "Host=localhost;Database=test;Username=test;Password=test";
        });

        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IIdempotencyStorage>()
            .Should()
            .BeOfType<PostgreSqlIdempotencyStorage>();
    }

    [Fact]
    public void AddCoreIdempotencyPostgreSql_Should_Throw_When_Configuration_Is_Null()
    {
        // Arrange
        var services = new ServiceCollection();
        // Act

        services.AddCoreIdempotency(options =>
        {
            options.Enabled = true;
            options.InstanceName = "TestInstance";
        });

        var action = () => services.AddCoreIdempotencyPostgreSql(options =>
        {
            options.ConnectionString = null;
        });

        // Assert
        action.Should()
              .Throw<InvalidOperationException>()
              .WithMessage(PostgreSqlMessages.PostgreSqlConnectionStringRequired);
    }

    [Fact]
    public void AddCoreIdempotencyPostgreSql_Should_Require_CoreIdempotency_Registration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var action = () => services.AddCoreIdempotencyPostgreSql(options =>
        {
            options.ConnectionString =
                "Host=localhost;Database=test;Username=test;Password=test";
        });

        // Assert
        action.Should()
              .Throw<InvalidOperationException>()
              .WithMessage(PostgreSqlMessages.IdempotencyRegistrationRequired);
    }
}
