using Core.Http.Abstractions;
using Core.Http.Responses;
using Core.Idempotency.Abstractions;
using Core.Idempotency.Constants;
using Core.Idempotency.Diagnostics;
using Core.Idempotency.Exceptions;
using Core.Idempotency.Fingerprint;
using Core.Idempotency.Internal;
using Core.Idempotency.Models;
using Core.Idempotency.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Core.Idempotency.Services;

internal sealed class IdempotencyService(
    IOptions<IdempotencyOptions> options,
    IdempotencyMetrics metrics,
    IIdempotencyStorage storage,
    IIdempotencyKeyResolver keyResolver,
    IRequestFingerprintProvider fingerprintProvider,
    IResponseCapture responseCapture,
    IHttpResponseWriter responseWriter)
    : IIdempotencyService
{
    private readonly IdempotencyOptions _options = options.Value;
    private readonly IdempotencyMetrics _metrics = metrics;
    private readonly IIdempotencyStorage _storage = storage;
    private readonly IIdempotencyKeyResolver _keyResolver = keyResolver;
    private readonly IRequestFingerprintProvider _fingerprintProvider = fingerprintProvider;
    private readonly IResponseCapture _responseCapture = responseCapture;
    private readonly IHttpResponseWriter _responseWriter = responseWriter;

    public async Task HandleAsync(
        HttpContext context,
        RequestDelegate next,
        CancellationToken cancellationToken = default)
    {
        var request = ResolveRequest(context);

        if (request is null)
        {
            await next(context);
            return;
        }

        RequestFingerprint? requestFingerprint = null;

        if (_options.Fingerprint.Enabled)
        {
            try
            {
                requestFingerprint = await _fingerprintProvider.ComputeAsync(context, cancellationToken);
            }
            catch (IdempotencyRequestBodyTooLargeException)
            {
                await next(context);
                return;
            }
        }

        _metrics.RecordRequest();

        var acquisition = await _storage.TryAcquireAsync(
            request.Key,
            _options.InProgressExpiration,
            cancellationToken);

        if (acquisition.Status is IdempotencyAcquireStatus.Completed)
        {
            await ResolveCompletedAsync(context, acquisition.Entry!, requestFingerprint, cancellationToken);
            return;
        }

        if (acquisition.Status is IdempotencyAcquireStatus.InProgress)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.Headers.Append("Retry-After", "1");
            return;
        }

        _metrics.RecordMiss();

        await ExecuteRequestAsync(
            context,
            request,
            acquisition.LeaseId!,
            requestFingerprint,
            next,
            cancellationToken);
    }

    private async Task ResolveCompletedAsync(
        HttpContext context,
        IdempotencyEntry entry,
        RequestFingerprint? requestFingerprint,
        CancellationToken cancellationToken)
    {
        if (entry.RequestFingerprint is not null &&
            requestFingerprint is not null &&
            entry.RequestFingerprint != requestFingerprint)
        {
            throw new IdempotencyFingerprintMismatchException();
        }

        _metrics.RecordHit();
        _metrics.RecordReplay();
        await ReplayResponseAsync(context, entry.Response, cancellationToken);
    }

    private IdempotencyContext? ResolveRequest(
        HttpContext context)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        if (!_options.AllowedMethods.Contains(context.Request.Method))
        {
            return null;
        }

        if (_options.ExcludedContentTypePrefixes.Any(prefix =>
                context.Request.ContentType?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true) ||
            context.Request.ContentLength > _options.MaxRequestBodySizeBytes)
        {
            return null;
        }

        if (!_keyResolver.TryResolve(context, out var key))
        {
            return null;
        }

        return new IdempotencyContext
        {
            Key = key!,
            Expiration = _options.Expiration
        };
    }

    private async Task ExecuteRequestAsync(
        HttpContext context,
        IdempotencyContext request,
        string leaseId,
        RequestFingerprint? requestFingerprint,
        RequestDelegate next,
        CancellationToken cancellationToken = default)
    {
        CapturedResponse response;

        try
        {
            response = await _responseCapture.CaptureAsync(context, next, cancellationToken);
        }
        catch
        {
            await _storage.ReleaseAsync(request.Key, leaseId, CancellationToken.None);
            throw;
        }

        if (!_options.CacheableStatusCodes.Contains(response.StatusCode))
        {
            await _storage.ReleaseAsync(request.Key, leaseId, cancellationToken);
            return;
        }

        if (response.Body.LongLength > _options.MaxResponseBodySizeBytes)
        {
            await _storage.ReleaseAsync(request.Key, leaseId, cancellationToken);
            return;
        }

        await PersistResponseAsync(
            request,
            leaseId,
            response,
            requestFingerprint,
            cancellationToken);
    }

    private async Task PersistResponseAsync(
        IdempotencyContext request,
        string leaseId,
        CapturedResponse response,
        RequestFingerprint? requestFingerprint,
        CancellationToken cancellationToken = default)
    {
        await _storage.CompleteAsync(
            request.Key,
            leaseId,
            new IdempotencyEntry
            {
                RequestFingerprint = requestFingerprint,
                Response = new IdempotencyResponse
                {
                    StatusCode = response.StatusCode,
                    ContentType = response.ContentType,
                    Body = response.Body,
                    Headers = response.Headers
                }
            },
            request.Expiration,
            cancellationToken);
    }

    private async Task ReplayResponseAsync(
        HttpContext context,
        IdempotencyResponse cached,
        CancellationToken cancellationToken = default)
    {
        context.Response.Headers.Append(
            HeaderNames.IdempotencyCache,
            HeaderValues.Hit);

        await _responseWriter.WriteAsync(
            context,
            new CapturedResponse
            {
                StatusCode = cached.StatusCode,
                Body = cached.Body,
                ContentType = cached.ContentType,
                Headers = cached.Headers,
            },
            cancellationToken);
    }
}
