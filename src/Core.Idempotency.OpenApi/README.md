# ⚡ CoreSystem.Idempotency.OpenApi

> **OpenAPI/Swagger integration for CoreSystem.Idempotency that documents the `Idempotency-Key` header on idempotent operations.**

Consumers of your API should discover the `Idempotency-Key` header from the generated OpenAPI document, not from documentation written by hand. This package adds the header to every operation whose HTTP method is idempotent, so it appears in Swagger UI and in the exported spec.

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

---

## 📦 Installation

Install the OpenAPI package. It depends on `CoreSystem.Idempotency` and `Swashbuckle.AspNetCore.SwaggerGen`, which are restored automatically.

```bash
dotnet add package CoreSystem.Idempotency.OpenApi
```

---

## 🚀 Quick Start

Register the core framework first, so `IdempotencyOptions` is available to the filter.

```csharp
builder.Services.AddCoreIdempotency(options =>
{
    options.Enabled = true;
});
```

Register the filter with Swashbuckle.

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.OperationFilter<IdempotencyHeaderOperationFilter>();
});
```

The filter resolves `IOptions<IdempotencyOptions>` from the service provider. Calling `AddCoreIdempotency` provides it, so the filter needs no separate registration.

---

## 🧩 How the Header Is Documented

`IdempotencyHeaderOperationFilter` implements `Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter`. For each operation it reads `OperationFilterContext.ApiDescription.HttpMethod`. When idempotency is enabled and the method is in `AllowedMethods`, it appends an `OpenApiParameter` named `Idempotency-Key` to the operation. The parameter is a header, optional, with a `string` schema in `uuid` format. Its description lists the allowed methods.

The filter adds nothing when `IdempotencyOptions.Enabled` is `false`, or when the method is not in `AllowedMethods`.

---

## 🏗 Architecture

The package is a thin layer over the core framework. It holds no state of its own: the filter reads the same `IdempotencyOptions` that the middleware uses, so the documented header always matches the runtime behavior.

```text
Swashbuckle SwaggerGen
        │
        ▼
IdempotencyHeaderOperationFilter
        │
        ▼
IOptions<IdempotencyOptions>   (CoreSystem.Idempotency)
```

---

## 📚 Documentation

- Full documentation site: https://federin.github.io/CoreSystem.Idempotency/OpenApi/
- Core package: [CoreSystem.Idempotency](https://www.nuget.org/packages/CoreSystem.Idempotency)

---

## 🤝 Contributing

Contributions are welcome. Please open an issue or pull request.

---

## 📄 License

Released under the MIT License.
