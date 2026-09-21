# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
and this project adheres to [Semantic Versioning](https://semver.org/).

---

## [2.0.0] - 2026-08-07

### Added

- Introduced a provider-based storage architecture.
- Added the `CoreSystem.Idempotency.Redis` package.
- Added the `CoreSystem.Idempotency.PostgreSql` package.
- Added the `IIdempotencyStorage` abstraction for pluggable storage providers.

### Changed

- Refactored `CoreSystem.Idempotency` to be completely storage-provider independent.
- Storage implementations are now distributed as independent NuGet packages.
- Simplified the core package by removing provider-specific dependencies.
- Updated documentation to reflect the new modular architecture.

### Removed

- Removed built-in Redis storage implementation from `CoreSystem.Idempotency`.
- Removed built-in PostgreSQL storage implementation from `CoreSystem.Idempotency`.

### Migration

Applications upgrading from **1.x** must:

1. Install a storage provider package:
   - `CoreSystem.Idempotency.Redis`, or
   - `CoreSystem.Idempotency.PostgreSql`.
2. Register the selected provider during application startup.
3. Remove any legacy provider configuration from `CoreSystem.Idempotency`.
