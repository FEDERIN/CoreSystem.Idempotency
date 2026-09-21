using System.Text;
using Core.Idempotency.Options;
using Core.Idempotency.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Core.Idempotency.Fingerprint;

internal sealed class RequestFingerprintBuilder(
    IOptions<IdempotencyOptions> options)
    : IRequestFingerprintBuilder
{
    private readonly FingerprintOptions _options =
        options.Value.Fingerprint;
    private readonly IdempotencyOptions _idempotencyOptions = options.Value;

    public async ValueTask<string> BuildAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var request = context.Request;

        if (request.ContentLength is > 0 && request.ContentLength > _idempotencyOptions.MaxRequestBodySizeBytes)
        {
            throw new IdempotencyRequestBodyTooLargeException();
        }

        if (!request.Body.CanSeek)
        {
            request.EnableBuffering(bufferThreshold: 30 * 1024, bufferLimit: _idempotencyOptions.MaxRequestBodySizeBytes);
        }

        var builder = new StringBuilder();

        builder.AppendLine(request.Method);
        builder.AppendLine(request.Path.Value ?? string.Empty);

        if (_options.IncludeQueryString)
        {
            builder.AppendLine(
                request.QueryString.Value ?? string.Empty);
        }

        foreach (var headerName in _options.IncludedHeaders.Order())
        {
            if (request.Headers.TryGetValue(headerName, out var value))
            {
                builder.Append(headerName);
                builder.Append('=');
                builder.AppendLine(value.ToString());
            }
        }

        if (_options.IncludeContentType)
        {
            builder.AppendLine(
                request.ContentType ?? string.Empty);
        }

        request.Body.Position = 0;

        var body = await ReadBodyAsync(request.Body, cancellationToken);

        builder.Append(body);

        request.Body.Position = 0;

        return builder.ToString();
    }

    private async Task<string> ReadBodyAsync(Stream body, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[8192];
        int read;

        while ((read = await body.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > _idempotencyOptions.MaxRequestBodySizeBytes)
            {
                throw new IdempotencyRequestBodyTooLargeException();
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        return Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
    }
}
