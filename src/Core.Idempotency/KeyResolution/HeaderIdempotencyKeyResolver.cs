using Core.Idempotency.Abstractions;
using Core.Idempotency.Constants;
using Core.Idempotency.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Core.Idempotency.KeyResolution;

internal sealed class HeaderIdempotencyKeyResolver(
    IOptions<IdempotencyOptions> options)
    : IIdempotencyKeyResolver
{
    private readonly IdempotencyOptions _options = options.Value;

    public bool TryResolve(
        HttpContext context,
        out string key)
    {
        if (!context.Request.Headers.TryGetValue(HeaderNames.IdempotencyKey, out var idempotency) ||
            idempotency.Count != 1)
        {
            key = string.Empty;
            return false;
        }

        var clientKey = idempotency[0];

        if (string.IsNullOrWhiteSpace(clientKey) ||
            clientKey.Length > _options.MaxIdempotencyKeyLength ||
            !string.Equals(clientKey, clientKey.Trim(), StringComparison.Ordinal))
        {
            key = string.Empty;
            return false;
        }

        var scope = _options.ScopeResolver?.Invoke(context)
                    ?? $"{_options.InstanceName}:{context.Request.Host.Value}";

        if (string.IsNullOrWhiteSpace(scope))
        {
            key = string.Empty;
            return false;
        }

        var material = string.Join('\n', scope, context.Request.Method, context.Request.Path, clientKey);
        key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material)));

        return true;
    }
}
