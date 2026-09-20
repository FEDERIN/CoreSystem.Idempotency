# 🔴 CoreSystem.Idempotency.Redis

![NuGet](https://img.shields.io/nuget/v/CoreSystem.Idempotency.Redis?style=for-the-badge)
![Downloads](https://img.shields.io/nuget/dt/CoreSystem.Idempotency.Redis?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![.NET](https://img.shields.io/badge/.NET-8.0-blue?style=for-the-badge)
![Redis](https://img.shields.io/badge/Redis-7+-DC382D?style=for-the-badge&logo=redis&logoColor=white)

Redis storage provider for **CoreSystem.Idempotency**.

CoreSystem.Idempotency.Redis provides a high-performance implementation of `IIdempotencyStorage`, enabling distributed idempotency across multiple ASP.NET Core application instances.

It is the recommended provider for production workloads that require low latency, horizontal scalability, and automatic expiration of idempotency entries.

---

# ✨ Features

- ✅ Distributed idempotency
- ✅ High-performance Redis storage
- ✅ Automatic expiration
- ✅ Horizontal scalability
- ✅ Seamless integration with CoreSystem.Idempotency
- ✅ OpenTelemetry compatible
- ✅ Production-ready

---

# 📦 Installation

Install the framework.

```bash
dotnet add package CoreSystem.Idempotency
```

Install the Redis provider.

```bash
dotnet add package CoreSystem.Idempotency.Redis
```

---

# 🚀 Quick Start

Configure Redis.

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

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

Register the Redis provider.

```csharp
builder.Services.AddCoreIdempotencyRedis();
```

Enable the middleware.

```csharp
app.UseCoreIdempotency();
```

---

# ⚡ Why Redis?

Redis is ideal for:

- Distributed APIs
- Kubernetes deployments
- Microservices
- High request throughput
- Low-latency workloads

---

# 📊 Characteristics

| Feature | Supported |
|----------|:---------:|
| Distributed | ✅ |
| Automatic Expiration | ✅ |
| Low Latency | ✅ |
| Horizontal Scaling | ✅ |
| Durable Persistence | ⚪ (Depends on Redis configuration) |

---

# 📚 Documentation

Complete documentation includes:

- Installation
- Configuration
- Production recommendations
- Performance considerations
- Architecture

---

# 📄 License

MIT License © Federin Pastor Gutierrez Ortiz