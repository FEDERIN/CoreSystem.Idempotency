using Core.Idempotency.DependencyInjection;
using Core.Idempotency.Redis.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Core.Idempotency.Redis.IntegrationTests.Fixtures;

public abstract class RedisIdempotencyTestBase : IAsyncDisposable
{
    private readonly TestServer _server;

    protected IServiceProvider Services { get; }

    protected HttpClient Client { get; }

    protected IConnectionMultiplexer Connection { get; }

    protected IDatabase Database => Connection.GetDatabase();

    protected RedisIdempotencyTestBase(
        RedisContainerFixture fixture,
        Action<IEndpointRouteBuilder> configureEndpoints)
    {
        Connection = ConnectionMultiplexer.Connect(
            fixture.ConnectionString);

        var builder = new WebHostBuilder();

        builder.ConfigureServices(services =>
        {
            services.AddRouting();

            services.AddCoreIdempotency(options =>
            {
                options.Enabled = true;
            });

            services.AddCoreIdempotencyRedis(options =>
            {
                options.Configuration = configuration =>
                {
                    configuration.EndPoints.Add(
                        fixture.ConnectionString);
                };
            });
        });

        builder.Configure(app =>
        {
            app
            .UseRouting()
            .UseCoreIdempotency()
            .UseEndpoints(configureEndpoints);
        });

        _server = new TestServer(builder);

        Client = _server.CreateClient();

        Services = _server.Services;
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();

        _server.Dispose();

        await Connection.DisposeAsync();

        GC.SuppressFinalize(this);
    }
}