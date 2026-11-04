# ⚡ CoreSystem.Idempotency.OpenApi

> **OpenAPI/Swagger integration for CoreSystem.Idempotency that documents the `Idempotency-Key` header on idempotent operations.**

OpenAPI/Swagger integration for CoreSystem.Idempotency. Documents the `Idempotency-Key` header on idempotent operations so consumers of your API discover the header from the generated spec.

![NuGet](https://img.shields.io/nuget/v/CoreSystem.Idempotency.OpenApi?style=for-the-badge)
![Downloads](https://img.shields.io/nuget/dt/CoreSystem.Idempotency.OpenApi?style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-blue?style=for-the-badge)

---

## ✨ Features

- ✅ Adds the `Idempotency-Key` header parameter to operations whose HTTP method is configured as idempotent.
- ✅ Respects `IdempotencyOptions.Enabled` and `AllowedMethods`.
- ✅ Works with Swashbuckle (`IOperationFilter`).
- ✅ Multi-targets net8.0, net9.0 and net10.0.

## 📦 Installation

```bash
dotnet add package CoreSystem.Idempotency.OpenApi
```

## 🚀 Quick Start

Register the filter with Swashbuckle:

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<IdempotencyHeaderOperationFilter>();
});
```

The filter resolves `IOptions<IdempotencyOptions>` from the service provider, so ensure idempotency is configured:

```csharp
builder.Services.AddIdempotency(options =>
{
    options.Enabled = true;
});
```

## 🏗 Architecture

`IdempotencyHeaderOperationFilter` implements `Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter`. It inspects `OperationFilterContext.ApiDescription.HttpMethod` and, when the method is in `AllowedMethods`, appends an `OpenApiParameter` named `Idempotency-Key` (header, string/uuid) to the operation.

## 📚 Documentation

- Full documentation site: https://federin.github.io/CoreSystem.Idempotency/
- Core package: CoreSystem.Idempotency

## 🤝 Contributing

Contributions are welcome. Please open an issue or pull request.

## 📄 License

Released under the MIT License.