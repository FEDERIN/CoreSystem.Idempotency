using Microsoft.AspNetCore.Http;
using Core.Idempotency.Abstractions;

namespace Core.Idempotency.Middleware;

internal sealed class IdempotencyMiddleware(
    RequestDelegate next,
    IIdempotencyService service)
{
    public Task InvokeAsync(HttpContext context)
        => service.HandleAsync(context, next, context.RequestAborted);
}