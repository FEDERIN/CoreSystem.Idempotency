using Microsoft.AspNetCore.Http;

namespace Core.Idempotency.Options;

/// <summary>
/// Configuration options for the Idempotency library.
/// </summary>
public class IdempotencyOptions
{
    /// <summary>
    /// Master switch to enable or disable the idempotency logic.
    /// Default is true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets the fingerprint generation configuration options.
    /// </summary>
    public FingerprintOptions Fingerprint { get; } = new();

    /// <summary>
    /// Gets or sets an optional prefix applied to all generated cache keys.
    /// </summary>
    /// <remarks>
    /// Using an instance name allows multiple applications or environments
    /// to safely share the same Redis instance without key collisions.
    /// </remarks>
    public string? InstanceName { get; set; }

    /// <summary>Maximum length accepted for the client supplied idempotency key.</summary>
    public int MaxIdempotencyKeyLength { get; set; } = 128;

    /// <summary>
    /// Resolves an application-specific scope, such as a tenant or authenticated user.
    /// The default scope uses <see cref="InstanceName"/> and the request host.
    /// </summary>
    public Func<HttpContext, string?>? ScopeResolver { get; set; }

    /// <summary>
    /// The duration for which the response will be stored in the cache.
    /// Default is 30 minutes.
    /// </summary>
    public TimeSpan Expiration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Maximum time a request may own an idempotency key while it is executing.
    /// Requests arriving during this period receive a conflict response.
    /// </summary>
    public TimeSpan InProgressExpiration { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>Maximum request body size fingerprinted by the middleware.</summary>
    public long MaxRequestBodySizeBytes { get; set; } = 64 * 1024;

    /// <summary>Maximum response body size retained for idempotency replay.</summary>
    public long MaxResponseBodySizeBytes { get; set; } = 256 * 1024;

    /// <summary>Content type prefixes excluded from idempotency processing.</summary>
    public ISet<string> ExcludedContentTypePrefixes { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "multipart/" };

    /// <summary>
    /// The HTTP methods that will be intercepted by the idempotency middleware.
    /// Default is ["POST", "PUT"].
    /// </summary>
    public HashSet<string> AllowedMethods { get; } = ["POST", "PUT"];

    /// <summary>
    /// HTTP status codes that will be stored and replayed for
    /// subsequent idempotent requests.
    /// </summary>
    public HashSet<int> CacheableStatusCodes { get; } =
    [
        StatusCodes.Status200OK,
        StatusCodes.Status201Created,
        StatusCodes.Status202Accepted,
        StatusCodes.Status204NoContent
    ];

    /// <summary>
    /// Adds one or more cacheable HTTP status codes.
    /// </summary>
    public void AddCacheableStatusCodes(
        params int[] statusCodes)
    {
        CacheableStatusCodes.UnionWith(statusCodes);
    }

    /// <summary>
    /// Removes one or more cacheable HTTP status codes.
    /// </summary>
    public void RemoveCacheableStatusCodes(
        params int[] statusCodes)
    {
        foreach (var statusCode in statusCodes)
        {
            CacheableStatusCodes.Remove(statusCode);
        }
    }

    /// <summary>
    /// Adds one or more allowed HTTP methods.
    /// </summary>
    public void AddAllowedMethods(
        params string[] methods)
    {
        AllowedMethods.UnionWith(
            methods.Select(m => m.ToUpperInvariant()));
    }

    /// <summary>
    /// Removes one or more allowed HTTP methods.
    /// </summary>
    public void RemoveAllowedMethods(
        params string[] methods)
    {
        foreach (var method in methods)
        {
            AllowedMethods.Remove(
                method.ToUpperInvariant());
        }
    }

    /// <summary>
    /// Copies the configuration from another IdempotencyOptions instance.
    /// </summary>
    /// <param name="source"></param>
    public void CopyFrom(IdempotencyOptions source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Enabled = source.Enabled;
        InstanceName = source.InstanceName;
        MaxIdempotencyKeyLength = source.MaxIdempotencyKeyLength;
        ScopeResolver = source.ScopeResolver;
        Expiration = source.Expiration;
        InProgressExpiration = source.InProgressExpiration;
        MaxRequestBodySizeBytes = source.MaxRequestBodySizeBytes;
        MaxResponseBodySizeBytes = source.MaxResponseBodySizeBytes;
        ExcludedContentTypePrefixes.Clear();
        foreach (var prefix in source.ExcludedContentTypePrefixes)
        {
            ExcludedContentTypePrefixes.Add(prefix);
        }
        Fingerprint.CopyFrom(source.Fingerprint);
        AllowedMethods.Clear();
        AllowedMethods.UnionWith(source.AllowedMethods);
        CacheableStatusCodes.Clear();
        CacheableStatusCodes.UnionWith(source.CacheableStatusCodes);
    }
}
