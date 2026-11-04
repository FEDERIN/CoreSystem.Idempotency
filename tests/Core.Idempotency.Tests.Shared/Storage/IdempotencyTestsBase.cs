using Core.Idempotency.Constants;
using FluentAssertions;

namespace Core.Idempotency.Tests.Shared.Storage;

public abstract class IdempotencyTestsBase
{
    protected abstract HttpClient Client { get; }

    [Fact]
    public async Task Should_ReturnCachedResponse_When_RequestIsRepeated()
    {
        var response1 = await ResponseOrders();
        var response2 = await ResponseOrders();

        response2.Headers.Should().Contain(h => h.Key == HeaderNames.IdempotencyCache);
    }

    private async Task<HttpResponseMessage> ResponseOrders()
    {
        HttpRequestMessage request = new(HttpMethod.Post, "/orders");
        request.Headers.Add(HeaderNames.IdempotencyKey, "356");

        var response = await Client.SendAsync(request, TestContext.Current.CancellationToken);

        return response;
    }
}
