using Microsoft.AspNetCore.Http;

namespace Core.Idempotency.Abstractions;

public interface IIdempotencyKeyResolver
{
    bool TryResolve(
        HttpContext context,
        out string key);
}