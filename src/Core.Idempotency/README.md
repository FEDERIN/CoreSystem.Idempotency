# ⚡ CoreSystem.Idempotency

![NuGet](https://img.shields.io/nuget/v/CoreSystem.Idempotency?style=for-the-badge)
![Downloads](https://img.shields.io/nuget/dt/CoreSystem.Idempotency?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![.NET](https://img.shields.io/badge/.NET-8.0-blue?style=for-the-badge)
![OpenTelemetry](https://img.shields.io/badge/OpenTelemetry-Enabled-purple?style=for-the-badge)

A production-ready idempotency framework for **ASP.NET Core** that suppresses concurrent duplicates and replays persisted responses for distributed applications.

CoreSystem.Idempotency transparently intercepts incoming requests, validates request fingerprints, persists successful responses through pluggable storage providers, and safely replays completed operations.

Designed around a provider-based architecture, the framework remains completely independent from the underlying persistence technology while providing built-in OpenTelemetry instrumentation and production-ready defaults.

---

# ✨ Features

- ✅ ASP.NET Core middleware
- ✅ Concurrent duplicate suppression and response replay
- ✅ Request fingerprint validation
- ✅ Automatic response replay
- ✅ Duplicate request detection
- ✅ Provider-independent storage architecture
- ✅ Configurable request fingerprinting
- ✅ Configurable expiration
- ✅ Configurable HTTP methods
- ✅ Built-in OpenTelemetry metrics
- ✅ Extensible through `IIdempotencyStorage`
- ✅ Production-ready architecture

---

# 📦 Available Storage Providers

CoreSystem.Idempotency requires a storage provider.

| Package | Description |
|----------|-------------|
| **CoreSystem.Idempotency.Redis** | Redis-based distributed storage |
| **CoreSystem.Idempotency.PostgreSql** | PostgreSQL-based durable storage |

Additional providers can be implemented by using the `IIdempotencyStorage` abstraction.

---

# 🚀 Quick Start

## 1. Install the framework

```bash
dotnet add package CoreSystem.Idempotency
```

## 2. Install a storage provider

### Redis

```bash
dotnet add package CoreSystem.Idempotency.Redis
```

### PostgreSQL

```bash
dotnet add package CoreSystem.Idempotency.PostgreSql
```

---

## 3. Register the framework

```csharp
builder.Services
    .AddCoreIdempotency(options =>
    {
        builder.Configuration
            .GetSection("Core:Idempotency")
            .Bind(options);
    });
```

Register the storage provider.

```csharp
builder.Services.AddCoreIdempotencyRedis();
```

or

```csharp
builder.Services.AddCoreIdempotencyPostgreSql();
```

---

## 4. Enable the middleware

```csharp
app.UseCoreIdempotency();
```

---

## 5. Configure the framework

```json
{
  "Core": {
    "Idempotency": {
      "Enabled": true,
      "InstanceName": "Orders",
      "Expiration": "06:00:00",
      "AllowedMethods": [
        "POST",
        "PUT"
      ]
    }
  }
}
```

---

# 🔒 How It Works

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

# 📊 Built-in Observability

CoreSystem.Idempotency publishes OpenTelemetry metrics out of the box.

Available metrics include:

- Request processing
- Cache hits and misses
- Response replay
- Storage read latency
- Storage write latency
- Persisted payload size

Compatible with:

- OpenTelemetry
- Prometheus
- Grafana
- Azure Monitor
- Jaeger
- Elastic
- Datadog

---

# 🏛️ Architecture

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

# 📚 Documentation

The complete documentation includes:

- Getting Started
- Architecture
- Configuration
- Request Lifecycle
- Fingerprinting
- Response Replay
- Storage Providers
- Observability
- Best Practices
- Error Reference
- Roadmap

---

# 🤝 Contributing

Contributions, bug reports, feature requests, and new storage providers are always welcome.

If you'd like to contribute:

1. Fork the repository.
2. Create a feature branch.
3. Submit a Pull Request.

---

# 📄 License

MIT License © Federin Pastor Gutierrez Ortiz
