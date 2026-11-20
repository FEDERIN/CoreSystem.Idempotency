# ⚡ CoreSystem.Idempotency

> **Production-ready idempotency for ASP.NET Core that suppresses concurrent duplicates and replays persisted responses.**

CoreSystem.Idempotency is a middleware-based idempotency framework for distributed applications. It fingerprints incoming requests, persists successful responses through pluggable storage providers, and replays completed operations instead of executing them twice.

![NuGet](https://img.shields.io/nuget/v/CoreSystem.Idempotency?style=for-the-badge)
![Downloads](https://img.shields.io/nuget/dt/CoreSystem.Idempotency?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-blue?style=for-the-badge)
![OpenTelemetry](https://img.shields.io/badge/OpenTelemetry-Enabled-purple?style=for-the-badge)

---

## ✨ Features

- ✅ ASP.NET Core middleware
- ✅ Concurrent duplicate suppression
- ✅ Response replay from persisted entries
- ✅ Request fingerprint validation
- ✅ Duplicate request detection
- ✅ Provider-independent storage architecture
- ✅ Configurable expiration, methods and fingerprinting
- ✅ Built-in OpenTelemetry metrics
- ✅ Extensible through `IIdempotencyStorage`

---

## 📦 Installation

Install the framework, then a storage provider. A provider is required.

```bash
dotnet add package CoreSystem.Idempotency
```

The package targets .NET 8, .NET 9 and .NET 10, and ships an assembly for each of those frameworks.

| Package | Purpose |
|---------|---------|
| [**CoreSystem.Idempotency.Redis**](https://www.nuget.org/packages/CoreSystem.Idempotency.Redis) | Redis-based distributed storage |
| [**CoreSystem.Idempotency.PostgreSql**](https://www.nuget.org/packages/CoreSystem.Idempotency.PostgreSql) | PostgreSQL-based durable storage |

Additional providers can be implemented through the `IIdempotencyStorage` abstraction.

---

## 🚀 Quick Start

Register the framework.

```csharp
builder.Services
    .AddCoreIdempotency(options =>
    {
        builder.Configuration
            .GetSection("Core:Idempotency")
            .Bind(options);
    });
```

Register a storage provider. Both providers require a configuration delegate.

```csharp
builder.Services.AddCoreIdempotencyRedis(options =>
{
    options.Configuration = redis =>
    {
        redis.EndPoints.Add("localhost:6379");
    };
});
```

```csharp
builder.Services.AddCoreIdempotencyPostgreSql(options =>
{
    options.ConnectionString =
        builder.Configuration.GetConnectionString("Idempotency");
});
```

Enable the middleware after routing and before the endpoints.

```csharp
app.UseCoreIdempotency();
```

Configure through the `Core:Idempotency` section.

```json
{
  "Core": {
    "Idempotency": {
      "Enabled": true,
      "InstanceName": "Orders",
      "Expiration": "06:00:00",
      "AllowedMethods": [ "POST", "PUT" ]
    }
  }
}
```

---

## 🔒 How It Works

```text
Incoming Request
        │
        ▼
Resolve Idempotency Key
        │
        ▼
Generate Request Fingerprint
        │
        ▼
Lookup IIdempotencyStorage
        │
  ┌─────┴─────┐
  │           │
Found      Not Found
  │           │
  ▼           ▼
Replay    Execute Endpoint
Response       │
              ▼
       Persist Response
```

A completed duplicate receives the stored response without executing the endpoint again. A request that arrives while the original is still running receives `409 Conflict` with `Retry-After: 1`. A request that reuses a key with a different fingerprint is rejected with `IdempotencyFingerprintMismatchException`.

---

## 📊 Built-in Observability

OpenTelemetry metrics are published out of the box through the `IdempotencyMetrics` meter: request count, cache hits and misses, response replays, storage write count, storage read and write duration, and response body size.

Metrics are exported through the standard OpenTelemetry exporters, so any OpenTelemetry-compatible backend can receive them.

---

## 🏗 Architecture

CoreSystem.Idempotency coordinates the idempotency workflow while remaining completely independent from storage implementations.

```text
ASP.NET Core
      │
      ▼
CoreSystem.Idempotency
      │
      ▼
IIdempotencyStorage
      │
 ┌────┴────┐
 ▼         ▼

Redis   PostgreSQL

Future Providers
```

This provider-based architecture allows new storage implementations to be introduced without modifying the core framework.

---

## 📚 Documentation

- [Getting Started](https://federin.github.io/CoreSystem.Idempotency/GettingStarted/)
- [Architecture](https://federin.github.io/CoreSystem.Idempotency/Architecture/) and [configuration](https://federin.github.io/CoreSystem.Idempotency/Configuration/)
- Storage providers: [Redis](https://federin.github.io/CoreSystem.Idempotency/Providers/Redis/) and [PostgreSQL](https://federin.github.io/CoreSystem.Idempotency/Providers/PostgreSQL/)
- [Delivery guarantees](https://federin.github.io/CoreSystem.Idempotency/Guarantees/)
- [Fingerprinting](https://federin.github.io/CoreSystem.Idempotency/Fingerprinting/) and [response replay](https://federin.github.io/CoreSystem.Idempotency/ResponseCaching/)
- [Observability](https://federin.github.io/CoreSystem.Idempotency/Observability/) and [error reference](https://federin.github.io/CoreSystem.Idempotency/Errors/idempotency-fingerprint-mismatch/)

---

## 🤝 Contributing

Contributions, bug reports, feature requests and new storage providers are always welcome. Fork the repository, create a feature branch, and open a pull request.

---

## 📄 License

Released under the MIT License.
