# ⚡ CoreSystem.Idempotency.Redis

> **Redis storage provider for CoreSystem.Idempotency, built on the `IIdempotencyStorage` abstraction.**

It is the recommended provider for production workloads that require low latency, horizontal scalability, and automatic expiration of idempotency entries.

![NuGet](https://img.shields.io/nuget/v/CoreSystem.Idempotency.Redis?style=for-the-badge)
![Downloads](https://img.shields.io/nuget/dt/CoreSystem.Idempotency.Redis?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![.NET](https://img.shields.io/badge/.NET-8.0-blue?style=for-the-badge)
![Redis](https://img.shields.io/badge/Redis-7+-DC382D?style=for-the-badge&logo=redis&logoColor=white)

---

## ✨ Features

- ✅ Distributed idempotency
- ✅ High-performance Redis storage
- ✅ Automatic expiration
- ✅ Horizontal scalability
- ✅ Seamless integration with CoreSystem.Idempotency
- ✅ OpenTelemetry compatible
- ✅ Production-ready

---

## 📦 Installation

Install the framework.

```bash
dotnet add package CoreSystem.Idempotency
```

Install the Redis provider.

```bash
dotnet add package CoreSystem.Idempotency.Redis
```

---

## 🚀 Quick Start

Configure the Redis connection and register the provider.

```csharp
builder.Services
    .AddCoreIdempotency(options =>
    {
        builder.Configuration
            .GetSection("Core:Idempotency")
            .Bind(options);
    })
    .AddCoreIdempotencyRedis(options =>
    {
        options.Configuration = redis =>
        {
            redis.EndPoints.Add("localhost:6379");
        };
    });
```

The connection endpoint is supplied through the `RedisOptions.Configuration` delegate,
which receives a `StackExchange.Redis.ConfigurationOptions` instance.

To read the endpoint from configuration instead, assign it inside the same delegate.

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

```csharp
builder.Services.AddCoreIdempotencyRedis(options =>
{
    options.Configuration = redis =>
    {
        redis.EndPoints.Add(
            builder.Configuration.GetConnectionString("Redis")!);
    };
});
```

> **Note**
>
> `RedisOptions.Configuration` is an `Action<ConfigurationOptions>`, not a string.
> A plain connection string cannot be bound to it with `Bind`.

Enable the middleware.

```csharp
app.UseCoreIdempotency();
```

---

## ⚡ Why Redis?

Redis is ideal for:

- Distributed APIs
- Kubernetes deployments
- Microservices
- High request throughput
- Low-latency workloads

---

## 📊 Characteristics

| Feature | Supported |
|----------|:---------:|
| Distributed | ✅ |
| Automatic Expiration | ✅ |
| Low Latency | ✅ |
| Horizontal Scaling | ✅ |
| Durable Persistence | ⚪ (Depends on Redis configuration) |

---

## 📚 Documentation

Complete documentation includes:

- Installation
- Configuration
- Production recommendations
- Performance considerations
- Architecture

---

## 🤝 Contributing

Contributions, bug reports and feature requests are always welcome. Fork the repository, create a feature branch, and open a pull request.

---

## 📄 License

Released under the MIT License.