using Core.Idempotency.DependencyInjection;
using Core.Idempotency.PostgreSql.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Idempotency.PostgreSql.IntegrationTests.Fixtures;

public abstract class PostgreSqlIdempotencyTestBase : IDisposable
{
    private readonly TestServer _server;

    protected IServiceProvider Services { get; }

    protected HttpClient Client { get; }

    protected PostgreSqlIdempotencyTestBase(
        PostgreSqlContainerFixture fixture,
        Action<IEndpointRouteBuilder> configureEndpoints)
    {
        var builder = new WebHostBuilder();

        builder.ConfigureServices(services =>
        {
            services.AddRouting();
            services.AddCoreIdempotency(options =>
            {
                options.Enabled = true;
                options.Fingerprint.Enabled = true;
                options.Fingerprint.IncludeQueryString = true;
                options.Fingerprint.IncludeContentType = true;
                options.Fingerprint.IncludedHeaders.Add("Authorization");
            });

            services.AddCoreIdempotencyPostgreSql(options =>
            {
                options.ConnectionString = fixture.ConnectionString;
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

    public void Dispose()
    {
        Client.Dispose();
        _server.Dispose();

        GC.SuppressFinalize(this);
    }
}