# ⚡ CoreSystem.Idempotency

> **Production-ready idempotency for ASP.NET Core that suppresses concurrent duplicates and replays persisted responses.**

CoreSystem.Idempotency is a middleware-based idempotency framework for distributed applications. It fingerprints incoming requests, persists successful responses through pluggable storage providers, and replays completed operations instead of executing them twice.

![NuGet](https://img.shields.io/nuget/v/CoreSystem.Idempotency?style=for-the-badge)
![Downloads](https://img.shields.io/nuget/dt/CoreSystem.Idempotency?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![.NET](https://img.shields.io/badge/.NET-8.0-blue?style=for-the-badge)
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

| Package | Purpose |
|---------|---------|
| **CoreSystem.Idempotency.Redis** | Redis-based distributed storage |
| **CoreSystem.Idempotency.PostgreSql** | PostgreSQL-based durable storage |

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

Enable the middleware.

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

Every duplicate request receives the original response without executing the application endpoint again.

---

## 📊 Built-in Observability

OpenTelemetry metrics are published out of the box: request processing, cache hits and misses, response replay, storage read and write latency, and persisted payload size.

Compatible with OpenTelemetry, Prometheus, Grafana, Azure Monitor, Jaeger, Elastic and Datadog.

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

- Getting Started
- Architecture and configuration
- Storage providers: Redis, PostgreSQL
- Delivery guarantees
- Fingerprinting and response replay
- Observability and error reference

---

## 🤝 Contributing

Contributions, bug reports, feature requests and new storage providers are always welcome. Fork the repository, create a feature branch, and open a pull request.

---

## 📄 License

Released under the MIT License.