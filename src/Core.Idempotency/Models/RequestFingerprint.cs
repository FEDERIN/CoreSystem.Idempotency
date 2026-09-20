namespace Core.Idempotency.Models;

/// <summary>
/// Represents the computed fingerprint of an HTTP request.
/// </summary>
public sealed record RequestFingerprint
{
    /// <summary>
    /// Gets the hash algorithm used to compute the fingerprint.
    /// </summary>
    public required string HashAlgorithm { get; init; }

    /// <summary>
    /// Gets the computed fingerprint value.
    /// </summary>
    public required string Value { get; init; }
}