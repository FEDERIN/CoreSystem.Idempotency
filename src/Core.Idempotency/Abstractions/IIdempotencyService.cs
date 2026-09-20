using Microsoft.AspNetCore.Http;

namespace Core.Idempotency.Abstractions;

internal interface IIdempotencyService
{
    Task HandleAsync(
        HttpContext context,
        RequestDelegate next,
        CancellationToken cancellationToken = default);
}