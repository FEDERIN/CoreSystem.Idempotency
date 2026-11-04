using Core.Idempotency.Options;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Core.Idempotency.OpenApi;

/// <summary>
/// Adds the <c>Idempotency-Key</c> header parameter to operations whose HTTP
/// method is configured as idempotent, so the header shows up in the
/// generated OpenAPI document.
/// </summary>
public sealed class IdempotencyHeaderOperationFilter : IOperationFilter
{
    private readonly IdempotencyOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdempotencyHeaderOperationFilter"/> class.
    /// </summary>
    /// <param name="options">The idempotency options.</param>
    public IdempotencyHeaderOperationFilter(IOptions<IdempotencyOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var currentMethod = context.ApiDescription.HttpMethod?.ToUpperInvariant();
        if (string.IsNullOrEmpty(currentMethod)
            || _options.AllowedMethods is null
            || !_options.AllowedMethods.Contains(currentMethod))
        {
            return;
        }

        operation.Parameters ??= [];

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Idempotency-Key",
            In = ParameterLocation.Header,
            Required = false,
            Schema = new OpenApiSchema
            {
                Type = "string",
                Format = "uuid"
            },
            Description = $"Idempotency header. Allowed methods: {string.Join(", ", _options.AllowedMethods.Distinct())}"
        });
    }
}