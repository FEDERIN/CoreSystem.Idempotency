using System.Text;
using Core.Idempotency.Abstractions;
using Core.Idempotency.Models;
using Core.Idempotency.Redis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Idempotency.Redis.IntegrationTests.Storage;

public sealed class RedisIdempotencyStorageTests(
    RedisContainerFixture fixture)
    : IClassFixture<RedisContainerFixture>
{
    private readonly RedisIdempotencyTestBaseImpl _fixture = new(
        fixture,
        ConfigureEndpoints);

    [Fact]
    public async Task SetAsync_Should_Not_Overwrite_When_Key_Already_Exists()
    {
        // Arrange
        var key = Guid.NewGuid().ToString();

        var storage = _fixture.Services
            .GetRequiredService<IIdempotencyStorage>();

        var entry = new IdempotencyEntry
        {
            Response = new IdempotencyResponse
            {
                StatusCode = 200,
                ContentType = "application/json",
                Body = Encoding.UTF8.GetBytes("""{"value":1}"""),
                Headers = new Dictionary<string, string[]>()
            }
        };

        // Primera escritura
        await storage.SetAsync(key, entry, ct: TestContext.Current.CancellationToken);

        var newEntry = new IdempotencyEntry
        {
            Response = new IdempotencyResponse
            {
                StatusCode = 500,
                ContentType = "application/json",
                Body = Encoding.UTF8.GetBytes("""{"value":2}"""),
                Headers = new Dictionary<string, string[]>()
            }
        };

        // Act
        await storage.SetAsync(key, newEntry, ct: TestContext.Current.CancellationToken);

        // Assert
        var stored = await storage.GetAsync(key, TestContext.Current.CancellationToken);

        stored.Should().NotBeNull();
        stored!.Response.StatusCode.Should().Be(200);
        stored.Response.ContentType.Should().Be("application/json");
        stored.Response.Body.Should()
            .Equal(Encoding.UTF8.GetBytes("""{"value":1}"""));
    }

    [Fact]
    public async Task TryAcquire_Should_Protect_InProgress_And_Replay_Binary_Response()
    {
        var storage = _fixture.Services.GetRequiredService<IIdempotencyStorage>();
        var key = Guid.NewGuid().ToString();
        var entry = new IdempotencyEntry
        {
            Response = new IdempotencyResponse
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

        owner.Status.Should().Be(IdempotencyAcquireStatus.Acquired);
        duplicate.Status.Should().Be(IdempotencyAcquireStatus.InProgress);
        completed.Should().BeTrue();
        replay.Status.Should().Be(IdempotencyAcquireStatus.Completed);
        replay.Entry!.Response.Body.Should().Equal(entry.Response.Body);
        replay.Entry.Response.Headers["X-Trace"].Should().Equal("trace-1");
    }

    [Fact]
    public async Task GetAsync_Should_Not_Return_Expired_Entry()
    {
        var storage = _fixture.Services.GetRequiredService<IIdempotencyStorage>();
        var key = Guid.NewGuid().ToString();
        var entry = new IdempotencyEntry
        {
            Response = new IdempotencyResponse { StatusCode = 200, Body = [], Headers = new Dictionary<string, string[]>() }
        };

        var ct = TestContext.Current.CancellationToken;
        await storage.SetAsync(key, entry, TimeSpan.FromMilliseconds(20), ct);
        await Task.Delay(100, ct);

        (await storage.GetAsync(key, ct)).Should().BeNull();
    }

    private static void ConfigureEndpoints(
        IEndpointRouteBuilder endpoints)
    {
    }

    private sealed class RedisIdempotencyTestBaseImpl(
        RedisContainerFixture fixture,
        Action<IEndpointRouteBuilder> configureEndpoints)
        : RedisIdempotencyTestBase(
            fixture,
            configureEndpoints)
    {
        public new IServiceProvider Services => base.Services;
    }
}
