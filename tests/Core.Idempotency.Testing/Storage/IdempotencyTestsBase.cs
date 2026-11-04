using FluentAssertions;

namespace Core.Idempotency.Testing.Storage;

public abstract class IdempotencyTestsBase
{
    protected abstract HttpClient Client { get; }

    [Fact]
    public async Task Should_ReturnCachedResponse_When_RequestIsRepeated()
    {
        var response1 = await ResponseOrders();
        var response2 = await ResponseOrders();

        response2.Headers.Should().Contain(h => h.Key == "X-Idempotency-Cache");
    }

    private async Task<HttpResponseMessage> ResponseOrders()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/orders");
        request.Headers.Add("Idempotency-Key", "123");

        return await Client.SendAsync(request, TestContext.Current.CancellationToken); ;
    }
}