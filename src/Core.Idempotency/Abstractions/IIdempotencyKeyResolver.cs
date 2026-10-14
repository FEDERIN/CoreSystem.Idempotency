using Microsoft.AspNetCore.Http;

namespace Core.Idempotency.Abstractions;

/// <summary>
/// Resolves the idempotency key supplied by the incoming request.
/// </summary>
/// <remarks>
/// The default implementation reads the <c>Idempotency-Key</c> header. Register a
/// replacement to source the key from elsewhere, such as a route value or a claim.
/// </remarks>
public interface IIdempotencyKeyResolver
{
    /// <summary>
    /// Attempts to read an idempotency key from the request.
    /// </summary>
    /// <param name="context">The request being processed.</param>
    /// <param name="key">
    /// The resolved key, or an empty string when the request carries none.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when a usable key was found; <see langword="false"/>
    /// when the request should be processed without idempotency handling.
    /// </returns>
    bool TryResolve(
        HttpContext context,
        out string key);
}