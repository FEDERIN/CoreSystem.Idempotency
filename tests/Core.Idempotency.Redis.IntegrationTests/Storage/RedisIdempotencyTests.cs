using Core.Idempotency.Redis.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using FluentAssertions;

namespace Core.Idempotency.Redis.IntegrationTests.Storage;

public sealed class RedisIdempotencyTests
    (RedisContainerFixture fixture)
        : IdempotencyTestsBase,
      IClassFixture<RedisContainerFixture>
{
    private static int _concurrentExecutions;

    private readonly RedisIdempotencyTestBaseImpl _fixture = new(
            fixture,
            ConfigureEndpoints);

    protected override HttpClient Client => _fixture.Client;

    [Fact]
    public async Task Should_Execute_Only_One_Request_When_Key_Is_In_Progress()
    {
        Interlocked.Exchange(ref _concurrentExecutions, 0);
        var key = Guid.NewGuid().ToString();

        var responses = await Task.WhenAll(SendConcurrentRequestAsync(key), SendConcurrentRequestAsync(key));

        responses.Select(response => response.StatusCode)
            .Should().BeEquivalentTo([System.Net.HttpStatusCode.OK, System.Net.HttpStatusCode.Conflict]);
        Volatile.Read(ref _concurrentExecutions).Should().Be(1);
    }

    private Task<HttpResponseMessage> SendConcurrentRequestAsync(string key)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/orders/concurrent");
        request.Headers.Add("Idempotency-Key", key);
        return Client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static void ConfigureEndpoints(
        IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/orders", () =>
            Results.Ok(new { Id = Guid.NewGuid() }));

        endpoints.MapPost("/orders/concurrent", async () =>
        {
            Interlocked.Increment(ref _concurrentExecutions);
            await Task.Delay(250);
            return Results.Ok();
        });
    }

    private sealed class RedisIdempotencyTestBaseImpl(
        RedisContainerFixture fixture,
        Action<IEndpointRouteBuilder> configureEndpoints)
                : RedisIdempotencyTestBase(fixture, configureEndpoints)
    {
        public new HttpClient Client => base.Client;
    }
}
