using Core.Idempotency.Abstractions;
using Core.Idempotency.PostgreSql.IntegrationTests.Fixtures;
using Dapper;
using FluentAssertions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Core.Idempotency.PostgreSql.IntegrationTests.Storage;

public sealed class PostgreSqlIdempotencyStorageTests(
    PostgreSqlContainerFixture fixture)
    : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlIdempotencyTestBaseImpl _fixture = new(
        fixture,
        ConfigureEndpoints);

    [Fact]
    public async Task GetAsync_Should_Throw_When_Fingerprint_Has_No_HashAlgorithm()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();

        await using var connection = new NpgsqlConnection(
            fixture.ConnectionString);

        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await connection.ExecuteAsync(
            """
            INSERT INTO idempotency_keys
            (
                key,
                request_fingerprint,
                hash_algorithm,
                status_code,
                content_type,
                headers,
                body,
                expires_at
            )
            VALUES
            (
                @Key,
                'fingerprint',
                NULL,
                200,
                'application/json',
                @Headers,
                @Body,
                NOW() + INTERVAL '1 day'
            );
            """,
            new
            {
                Key = key,
                Headers = Array.Empty<byte>(),
                Body = Array.Empty<byte>()
            });

        var storage = _fixture.Services
            .GetRequiredService<IIdempotencyStorage>();

        // Act
        var action = () => storage.GetAsync(key);

        // Assert
        await action.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Stored fingerprint does not contain a hash algorithm.");
    }

    [Fact]
    public async Task TryAcquire_Should_Protect_InProgress_And_Replay_Binary_Response()
    {
        var storage = _fixture.Services.GetRequiredService<IIdempotencyStorage>();
        var key = Guid.NewGuid().ToString();
        var entry = new Core.Idempotency.Models.IdempotencyEntry
        {
            Response = new Core.Idempotency.Models.IdempotencyResponse
            {
                StatusCode = 201,
                ContentType = "application/octet-stream",
                Body = [0, 255, 1, 128],
                Headers = new Dictionary<string, string[]> { ["X-Trace"] = ["trace-1"] }
            }
        };

        var ct = TestContext.Current.CancellationToken;
        var owner = await storage.TryAcquireAsync(key, TimeSpan.FromMinutes(1), ct);
        var duplicate = await storage.TryAcquireAsync(key, TimeSpan.FromMinutes(1), ct);
        var completed = await storage.CompleteAsync(key, owner.LeaseId!, entry, TimeSpan.FromMinutes(1), ct);
        var replay = await storage.TryAcquireAsync(key, TimeSpan.FromMinutes(1), ct);

        owner.Status.Should().Be(Core.Idempotency.Models.IdempotencyAcquireStatus.Acquired);
        duplicate.Status.Should().Be(Core.Idempotency.Models.IdempotencyAcquireStatus.InProgress);
        completed.Should().BeTrue();
        replay.Status.Should().Be(Core.Idempotency.Models.IdempotencyAcquireStatus.Completed);
        replay.Entry!.Response.Body.Should().Equal(entry.Response.Body);
        replay.Entry.Response.Headers["X-Trace"].Should().Equal("trace-1");
    }

    [Fact]
    public async Task DistributedSchema_Should_Contain_Lease_And_Response_Columns()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        var columns = await connection.QueryAsync<string>("""
            SELECT column_name FROM information_schema.columns
            WHERE table_name = 'idempotency_keys';
            """);

        columns.Should().Contain(["state", "lease_id", "request_fingerprint", "hash_algorithm", "headers", "body"]);
    }

    private static void ConfigureEndpoints(
        IEndpointRouteBuilder endpoints)
    {
    }

    private sealed class PostgreSqlIdempotencyTestBaseImpl(
        PostgreSqlContainerFixture fixture,
        Action<IEndpointRouteBuilder> configureEndpoints)
        : PostgreSqlIdempotencyTestBase(
            fixture,
            configureEndpoints)
    {
        public new IServiceProvider Services => base.Services;
    }
}
