using Core.Idempotency.KeyResolution;
using Core.Idempotency.Options;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace Core.Idempotency.UnitTests.KeyResolution;

public sealed class HeaderIdempotencyKeyResolverTests
{
    [Fact]
    public void TryResolve_Should_Reject_Multiple_Or_Whitespace_Keys()
    {
        var resolver = CreateResolver();
        var multiple = new DefaultHttpContext();
        multiple.Request.Headers.Append("Idempotency-Key", "one");
        multiple.Request.Headers.Append("Idempotency-Key", "two");
        var whitespace = new DefaultHttpContext();
        whitespace.Request.Headers["Idempotency-Key"] = " key ";

        resolver.TryResolve(multiple, out _).Should().BeFalse();
        resolver.TryResolve(whitespace, out _).Should().BeFalse();
    }

    [Fact]
    public void TryResolve_Should_Isolate_Tenants_And_Routes()
    {
        var resolver = CreateResolver(options => options.ScopeResolver = context => context.Request.Headers["X-Tenant"]);
        var first = CreateContext("tenant-a", "/orders");
        var second = CreateContext("tenant-b", "/orders");
        var otherRoute = CreateContext("tenant-a", "/payments");

        resolver.TryResolve(first, out var firstKey).Should().BeTrue();
        resolver.TryResolve(second, out var secondKey).Should().BeTrue();
        resolver.TryResolve(otherRoute, out var routeKey).Should().BeTrue();

        firstKey.Should().NotBe(secondKey).And.NotBe(routeKey);
    }

    private static HeaderIdempotencyKeyResolver CreateResolver(Action<IdempotencyOptions>? configure = null)
    {
        var options = new IdempotencyOptions { InstanceName = "test" };
        configure?.Invoke(options);
        return new HeaderIdempotencyKeyResolver(Microsoft.Extensions.Options.Options.Create(options));
    }

    private static DefaultHttpContext CreateContext(string tenant, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = path;
        context.Request.Headers["Idempotency-Key"] = "operation-1";
        context.Request.Headers["X-Tenant"] = tenant;
        return context;
    }
}
