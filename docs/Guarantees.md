# Delivery Guarantees

CoreSystem.Idempotency provides two HTTP-level guarantees for a configured idempotency key:

- Only one request may hold the in-progress lease at a time. Concurrent duplicates receive `409 Conflict` with `Retry-After`.
- Once a successful response has been persisted, later requests with the same key and fingerprint replay that response without executing the endpoint.

## What it cannot guarantee

The middleware cannot by itself guarantee exactly-once business effects across process crashes and external systems. For example, an endpoint can commit a payment or publish a message and the process can fail before the middleware persists its response. A retry may then execute the endpoint again after the lease expires.

## Exactly-once business effects

For operations where a duplicate effect is unacceptable, combine this middleware with one of these application-level patterns:

- Store the business operation and the idempotency result in the same database transaction.
- Use the transactional outbox pattern for messages and external side effects.
- Pass the idempotency key to downstream providers that support their own idempotency contract.
- Make the domain command naturally idempotent, for example by enforcing a unique business identifier.

Treat the idempotency key as part of the business command, rather than as the only protection against duplicate side effects.
