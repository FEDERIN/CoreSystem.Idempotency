namespace Core.Idempotency.Exceptions;

/// <summary>
/// The incoming request does not match the fingerprint
/// associated with the existing idempotency entry.
/// </summary>
public sealed class IdempotencyFingerprintMismatchException : Exception
{
    public const string Code = "IDEMPOTENCY_FINGERPRINT_MISMATCH";

    public const string Title = "Idempotency fingerprint mismatch";

    public const string Type =
        "https://federin.github.io/CoreSystem/Idempotency/Errors/idempotency-fingerprint-mismatch/";

    public IdempotencyFingerprintMismatchException()
        : base("The request fingerprint does not match the existing idempotency entry.")
    {
    }
}