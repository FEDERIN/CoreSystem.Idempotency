using Core.Idempotency.Constants;
using FluentAssertions;

namespace Core.Idempotency.Redis.IntegrationTests.Storage;

public abstract class IdempotencyTestsBase
{
    protected abstract HttpClient Client { get; }

    [Fact]
    public async Task Should_ReturnCachedResponse_When_RequestIsRepeated()
    {
        await ResponseOrders();
        var response2 = await ResponseOrders();

        response2.Headers.Should().Contain(h => h.Key == HeaderNames.IdempotencyCache);
    }

    private async Task<HttpResponseMessage> ResponseOrders()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/orders");
        request.Headers.Add(HeaderNames.IdempotencyKey, "123");

        return await Client.SendAsync(request, TestContext.Current.CancellationToken); ;
    }
}