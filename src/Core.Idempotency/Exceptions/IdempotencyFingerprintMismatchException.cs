namespace Core.Idempotency.Exceptions;

/// <summary>
/// The incoming request does not match the fingerprint
/// associated with the existing idempotency entry.
/// </summary>
public sealed class IdempotencyFingerprintMismatchException : Exception
{
    /// <summary>
    /// Stable machine-readable code identifying this error. The framework does
    /// not write this value to the response; an application that chooses to
    /// expose a problem details <c>code</c> member is responsible for
    /// including it.
    /// </summary>
    public const string Code = "IDEMPOTENCY_FINGERPRINT_MISMATCH";

    /// <summary>
    /// Short human-readable title for this error, exposed as the problem details
    /// <c>title</c> member.
    /// </summary>
    public const string Title = "Idempotency fingerprint mismatch";

    /// <summary>
    /// Documentation URI describing this error, exposed as the problem details
    /// <c>type</c> member.
    /// </summary>
    public const string Type =
        "https://federin.github.io/CoreSystem.Idempotency/Errors/idempotency-fingerprint-mismatch/";

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="IdempotencyFingerprintMismatchException"/> class.
    /// </summary>
    public IdempotencyFingerprintMismatchException()
        : base("The request fingerprint does not match the existing idempotency entry.")
    {
    }
}