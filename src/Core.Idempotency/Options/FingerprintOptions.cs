namespace Core.Idempotency.Options;

/// <summary>
/// Configuration options for request fingerprint generation and validation.
/// </summary>
public sealed class FingerprintOptions
{
    /// <summary>
    /// Enables request fingerprint validation.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Includes the query string in the fingerprint.
    /// </summary>
    public bool IncludeQueryString { get; set; }

    /// <summary>
    /// Includes the Content-Type header in the fingerprint.
    /// </summary>
    public bool IncludeContentType { get; set; } = true;

    /// <summary>
    /// Additional request headers to include in the fingerprint.
    /// Header names are treated case-insensitively.
    /// </summary>
    public ISet<string> IncludedHeaders { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Copies the values from another FingerprintOptions instance.
    /// </summary>
    /// <param name="source"></param>
    public void CopyFrom(FingerprintOptions source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Enabled = source.Enabled;
        IncludeQueryString = source.IncludeQueryString;
        IncludeContentType = source.IncludeContentType;
        IncludedHeaders.Clear();
        foreach (var header in source.IncludedHeaders)
        {
            IncludedHeaders.Add(header);
        }
    }
}