using Core.Idempotency.OpenApi;
using Core.Idempotency.Options;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Core.Idempotency.OpenApi.UnitTests;

public sealed class IdempotencyHeaderOperationFilterTests
{
    private static IdempotencyHeaderOperationFilter CreateFilter(bool enabled, params string[] allowedMethods)
    {
        var options = new IdempotencyOptions { Enabled = enabled };
        options.AllowedMethods.Clear();
        foreach (var m in allowedMethods) options.AllowedMethods.Add(m);
        return new IdempotencyHeaderOperationFilter(Microsoft.Extensions.Options.Options.Create(options));
    }

    [Fact]
    public void Apply_WhenDisabled_DoesNotAddHeader()
    {
        var filter = CreateFilter(false, "POST");
        var operation = new OpenApiOperation();
        var context = CreateContext("POST");
        filter.Apply(operation, context);
        operation.Parameters.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Apply_WhenMethodNotAllowed_DoesNotAddHeader()
    {
        var filter = CreateFilter(true, "POST");
        var operation = new OpenApiOperation();
        var context = CreateContext("GET");
        filter.Apply(operation, context);
        operation.Parameters.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Apply_WhenEnabledAndMethodAllowed_AddsIdempotencyKeyHeader()
    {
        var filter = CreateFilter(true, "POST", "PUT");
        var operation = new OpenApiOperation();
        var context = CreateContext("post");
        filter.Apply(operation, context);
        operation.Parameters.Should().ContainSingle(p => p.Name == "Idempotency-Key" && p.In == ParameterLocation.Header);
    }

    private static OperationFilterContext CreateContext(string httpMethod)
    {
        var apiDescription = new Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription { HttpMethod = httpMethod };
        return new OperationFilterContext(apiDescription, null!, null!, null!);
    }
}
