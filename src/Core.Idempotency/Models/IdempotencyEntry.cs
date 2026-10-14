namespace Core.Idempotency.Models;

/// <summary>
/// A stored idempotency entry: the request identity it belongs to, and the
/// response to replay for a matching duplicate.
/// </summary>
public sealed class IdempotencyEntry
{
    /// <summary>
    /// Gets the fingerprint recorded when the request was first processed.
    /// </summary>
    /// <remarks>
    /// A later request presenting the same key but a different fingerprint is a
    /// client error and results in
    /// <see cref="Exceptions.IdempotencyFingerprintMismatchException"/>.
    /// </remarks>
    public RequestFingerprint? RequestFingerprint { get; init; }

    /// <summary>
    /// Gets the persisted response replayed for duplicate requests.
    /// </summary>
    public required IdempotencyResponse Response { get; init; }
}