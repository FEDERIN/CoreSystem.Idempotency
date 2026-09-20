using System.Diagnostics.Metrics;

using System.Diagnostics;

namespace Core.Idempotency.Diagnostics;

/// <summary>
/// Provides telemetry metrics for the idempotency system.
/// </summary>
public sealed class IdempotencyMetrics
{
    private readonly Counter<long>? _cacheHitCounter;
    private readonly Counter<long>? _cacheMissCounter;
    private readonly Counter<long>? _requestCounter;
    private readonly Counter<long>? _replayCounter;
    private readonly Counter<long>? _storageWriteCounter;

    private readonly Histogram<double>? _storageReadDuration;
    private readonly Histogram<double>? _storageWriteDuration;
    private readonly Histogram<long>? _payloadSize;

    public IdempotencyMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(
            IdempotencyDiagnosticsConstants.MeterName,
            "1.0.0");

        _requestCounter = meter.CreateCounter<long>(
            name: "idempotency.requests",
            unit: "{requests}",
            description: "Total idempotency requests.");

        _cacheHitCounter = meter.CreateCounter<long>(
            name: "idempotency.cache.hits",
            unit: "{hits}",
            description: "Total requests served from idempotency storage.");

        _cacheMissCounter = meter.CreateCounter<long>(
            name: "idempotency.cache.misses",
            unit: "{misses}",
            description: "Total requests that required execution.");

        _replayCounter = meter.CreateCounter<long>(
            name: "idempotency.response.replays",
            unit: "{responses}",
            description: "Total cached responses replayed.");

        _storageWriteCounter = meter.CreateCounter<long>(
            name: "idempotency.storage.writes",
            unit: "{writes}",
            description: "Total responses persisted.");

        _storageReadDuration = meter.CreateHistogram<double>(
            name: "idempotency.storage.read.duration",
            unit: "ms",
            description: "Duration of storage read operations.");

        _storageWriteDuration = meter.CreateHistogram<double>(
            name: "idempotency.storage.write.duration",
            unit: "ms",
            description: "Duration of storage write operations.");

        _payloadSize = meter.CreateHistogram<long>(
            name: "idempotency.payload.size",
            unit: "By",
            description: "Serialized response payload size.");
    }

    public void RecordRequest() =>
        _requestCounter?.Add(1);

    public void RecordHit() =>
        _cacheHitCounter?.Add(1);

    public void RecordMiss() =>
        _cacheMissCounter?.Add(1);

    public void RecordReplay() =>
        _replayCounter?.Add(1);

    public void RecordStorageWrite(string provider) =>
        _storageWriteCounter?.Add(1, new TagList { { "provider", provider } });

    public void RecordStorageReadDuration(double milliseconds, string provider) =>
        _storageReadDuration?.Record(milliseconds, new TagList { { "provider", provider } });

    public void RecordStorageWriteDuration(double milliseconds, string provider) =>
        _storageWriteDuration?.Record(milliseconds, new TagList { { "provider", provider } });

    /// <summary>Records the HTTP response body size, not serialized storage size.</summary>
    public void RecordPayloadSize(long bytes, string provider) =>
        _payloadSize?.Record(bytes, new TagList { { "provider", provider } });
}
