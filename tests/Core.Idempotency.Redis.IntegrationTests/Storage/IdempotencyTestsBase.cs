using FluentAssertions;

namespace Core.Idempotency.Redis.IntegrationTests.Storage;

public abstract class IdempotencyTestsBase
{
    protected abstract HttpClient Client { get; }

    [Fact]
    public async Task Should_ReturnCachedResponse_When_RequestIsRepeated()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/orders");
        request.Headers.Add("Idempotency-Key", "123");

        var response1 = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        request = new HttpRequestMessage(HttpMethod.Post, "/orders");
        request.Headers.Add("Idempotency-Key", "123");

        var response2 = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        response2.Headers.Should().Contain(h => h.Key == "X-Idempotency-Cache");
    }
}