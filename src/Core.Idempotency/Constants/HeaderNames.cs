namespace Core.Idempotency.Constants;

/// <summary>
/// Well-known HTTP header names used by the idempotency framework.
/// </summary>
public static class HeaderNames
{
    /// <summary>
    /// The Idempotency-Key request header.
    /// </summary>
    public const string IdempotencyKey = "Idempotency-Key";

    /// <summary>
    /// The X-Idempotency-Cache response header.
    /// </summary>
    public const string IdempotencyCache = "X-Idempotency-Cache";
}