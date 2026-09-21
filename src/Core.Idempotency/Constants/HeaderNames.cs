namespace Core.Idempotency.Constants;

internal static class HeaderNames
{
    public const string IdempotencyKey = "Idempotency-Key";

    public const string IdempotencyCache = "X-Idempotency-Cache";
}