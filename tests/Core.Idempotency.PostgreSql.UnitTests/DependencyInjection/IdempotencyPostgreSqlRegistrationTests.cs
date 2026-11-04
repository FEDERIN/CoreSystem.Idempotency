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
        var services = CreateServices();

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
        var services = CreateServices();

        CreateServicesExpectingInvalidOperationException(
            services,
            null,
            PostgreSqlMessages.PostgreSqlConnectionStringRequired);
    }

    [Fact]
    public void AddCoreIdempotencyPostgreSql_Should_Require_CoreIdempotency_Registration()
    {
        var services = new ServiceCollection();

        CreateServicesExpectingInvalidOperationException(
            services,
            "Host=localhost;Database=test;Username=test;Password=test",
            PostgreSqlMessages.IdempotencyRegistrationRequired);
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
        string? connectionString,
        string message)
    {
        var action = () => services.AddCoreIdempotencyPostgreSql(options =>
        {
            options.ConnectionString = connectionString;
        });

        action.Should()
              .Throw<InvalidOperationException>()
              .WithMessage(message);
    }
}