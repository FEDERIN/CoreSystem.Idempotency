# Idempotency Fingerprint Mismatch

**HTTP Status:** `409 Conflict`

**Exception**

```text
IdempotencyFingerprintMismatchException
```

**Problem Details Type**

```text
https://github.com/FEDERIN/CoreSystem/blob/main/docs/Idempotency/errors/idempotency-fingerprint-mismatch.md
```

---

## Summary

This error occurs when an incoming request reuses an existing **Idempotency-Key**, but the generated request fingerprint does not match the fingerprint originally associated with that key.

A request fingerprint is a deterministic hash generated from the incoming request based on the configured fingerprint options.

By default, the fingerprint is generated from the request body, but it can also include additional request components such as:

- HTTP method
- Request path
- Query string
- Content-Type
- Selected request headers

For safety, **CoreSystem.Idempotency** only allows an idempotency key to be reused when the incoming request is considered identical to the original request.

---

## Example

The following request is processed successfully and stored.

```http
POST /orders
Idempotency-Key: 15
Content-Type: application/json

{
    "amount": 100
}
```

Later, another request reuses the same idempotency key but changes the request payload.

```http
POST /orders
Idempotency-Key: 15
Content-Type: application/json

{
    "amount": 200
}
```

Since the generated fingerprint no longer matches the stored fingerprint, the middleware throws an `IdempotencyFingerprintMismatchException`.

Applications typically translate this exception into a `409 Conflict` response.

An ASP.NET Core application using **Problem Details** may produce a response similar to the following:

```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json
```

```json
{
  "type": "https://github.com/FEDERIN/CoreSystem/blob/main/docs/Idempotency/errors/idempotency-fingerprint-mismatch.md",
  "title": "Idempotency fingerprint mismatch",
  "status": 409,
  "detail": "The request fingerprint does not match the existing idempotency entry.",
  "idempotencyKey": "15"
}
```

> **Note**
>
> `Core.Idempotency` only throws the exception.
> The application is responsible for converting it into an HTTP response.

---

## ASP.NET Core Integration

Applications using ASP.NET Core can translate the exception into an RFC 7807 **Problem Details** response by registering an `IExceptionHandler`.

```csharp
using Core.Idempotency.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

internal sealed class IdempotencyExceptionHandler(
    IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService = problemDetailsService;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not IdempotencyFingerprintMismatchException)
        {
            return false;
        }

        var problem = new ProblemDetails
        {
            Type = IdempotencyFingerprintMismatchException.Type,
            Title = IdempotencyFingerprintMismatchException.Title,
            Status = StatusCodes.Status409Conflict,
            Detail = exception.Message
        };

        problem.Extensions["idempotencyKey"] =
            context.Request.Headers["Idempotency-Key"].ToString();

        context.Response.StatusCode = problem.Status.GetValueOrDefault();

        return await _problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = problem,
                Exception = exception
            });
    }
}
```

Register the exception handler during application startup.

```csharp
builder.Services.AddExceptionHandler<IdempotencyExceptionHandler>();
```

> **Note**
>
> Applications that already implement a centralized exception handling pipeline (for example, a custom exception middleware or exception mapper) can map `IdempotencyFingerprintMismatchException` using their existing infrastructure instead of registering this handler.

---

## Common Causes

This error is commonly caused by one of the following:

- The request body changed.
- The HTTP method changed.
- The request path changed.
- The `Content-Type` header changed.
- A configured request header changed.
- The query string changed (when included in the fingerprint).
- Different `FingerprintOptions` are being used between requests.

---

## Resolution

Choose one of the following approaches:

- Resend the **exact same request** using the existing idempotency key.
- Generate a **new idempotency key** if the request represents a different business operation.
- Verify the configured `FingerprintOptions` if identical requests unexpectedly produce different fingerprints.

---

## Why Is This Validation Required?

An idempotency key represents a single logical operation.

Allowing different requests to reuse the same idempotency key could result in:

- Returning an incorrect cached response.
- Executing an unintended business operation.
- Data inconsistencies.
- Duplicate financial or transactional operations.

To prevent these scenarios, **CoreSystem.Idempotency** validates both:

- The idempotency key.
- The generated request fingerprint.

Only when both values match can the previously stored response be safely replayed.

---

## Related Documentation

- Fingerprinting
- Response Replay
- Configuration
- Redis Provider
- PostgreSQL Provider