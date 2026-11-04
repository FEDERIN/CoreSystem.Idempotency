using Core.Idempotency.Constants;
using FluentAssertions;

namespace Core.Idempotency.PostgreSql.IntegrationTests.Storage;

public abstract class IdempotencyTestsBase
{
    protected abstract HttpClient Client { get; }

    [Fact]
    public async Task Should_ReturnCachedResponse_When_RequestIsRepeated()
    {
        var request = RequestOrders();

        var response1 = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        request = RequestOrders();

        var response2 = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        response2.Headers.Should().Contain(h => h.Key == HeaderNames.IdempotencyCache);
    }

    private static HttpRequestMessage RequestOrders()
    {
        HttpRequestMessage request = new(HttpMethod.Post, "/orders");
        request.Headers.Add(HeaderNames.IdempotencyKey, "123");
        return request;
    }
}