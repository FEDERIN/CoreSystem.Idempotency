namespace Core.Idempotency.Models;

/// <summary>
/// Represents a cached HTTP response associated with an idempotency key.
/// </summary>
public sealed class IdempotencyResponse
{
    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public required int StatusCode { get; init; }

    /// <summary>
    /// Gets the HTTP content type.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets the response body.
    /// </summary>
    public required byte[] Body { get; init; }

    /// <summary>
    /// Gets the response headers captured from the original response.
    /// </summary>
    public required IReadOnlyDictionary<string, string[]> Headers
    {
        get;
        init;
    }
}