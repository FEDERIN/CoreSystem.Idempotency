# AGENTS.md

Instructions for AI agents (and humans) working in this repository.

## Memory

- At the start, read `MEMORY.md` to learn the current state of the project and past decisions.
- When a task is finished, update it: current state, important decisions (with their rationale), and mistakes to avoid.
- Keep it short (max ~50 lines): summarize or drop what no longer adds value.
- If something becomes a permanent rule, propose moving it to `AGENTS.md` instead of keeping it in memory.
- Never store sensitive data (keys, tokens, personal data).

## Project

**CoreSystem.Idempotency** — a production-ready idempotency framework for ASP.NET Core (.NET 8).
It suppresses concurrent duplicate requests, fingerprints requests, persists successful
responses through pluggable storage providers, and replays them on retry.

Repository: `FEDERIN/CoreSystem.Idempotency` · License: MIT · Docs: https://federin.github.io/CoreSystem.Idempotency/

## Environment

| Item | Value |
|------|-------|
| .NET SDK | Pinned to **8.0.402** via `global.json` (do not change unless the repo is upgraded deliberately) |
| Target framework | `net8.0` for every project |
| Solution | `CoreSystem.Idempotency.sln` |
| Nullable | `enable` everywhere |
| Implicit usings | `enable` |
| Packaging | Central Package Management via `Directory.Packages.props` with transitive pinning |
| Lock files | `packages.lock.json` committed per project; CI restores with `--locked-mode` |

`src/Directory.Build.props` holds shared NuGet metadata for the shipped packages:
`GenerateDocumentationFile`, SourceLink and deterministic builds. There is **no** root
`Directory.Build.props` and **no** `.editorconfig`, so do not assume `TreatWarningsAsErrors`,
`AnalysisLevel`, or `dotnet format` rules — they are not configured.

## Repository layout

```text
src/
  Core.Idempotency/                     -> package CoreSystem.Idempotency           (core, no storage deps)
  Core.Idempotency.Redis/               -> package CoreSystem.Idempotency.Redis
  Core.Idempotency.PostgreSql/          -> package CoreSystem.Idempotency.PostgreSql
tests/
  Core.Idempotency.UnitTests/           -> core unit tests
  Core.Idempotency.Redis.UnitTests/     -> Redis provider unit tests (DI)
  Core.Idempotency.PostgreSql.UnitTests/-> PostgreSQL provider unit tests (DI)
  Core.Idempotency.Redis.IntegrationTests/          -> Testcontainers-based
  Core.Idempotency.PostgreSql.IntegrationTests/     -> Testcontainers-based
docs/                                   -> MkDocs Material site
mkdocs.yml, requirements.txt, serve-docs.bat
generate-lock-files.bat, publish-package.bat
```

### Naming rules (important, they are inconsistent-looking but intentional)

- **Assembly / folder / project**: `Core.Idempotency*`
- **Namespace**: `Core.Idempotency.*` (e.g. `Core.Idempotency.Middleware`)
- **NuGet PackageId**: `CoreSystem.Idempotency*`
- **Public API prefix**: `Core` — `AddCoreIdempotency()`, `UseCoreIdempotency()`,
  `AddCoreIdempotencyRedis(Action<RedisOptions>)`,
  `AddCoreIdempotencyPostgreSql(Action<PostgreSqlOptions>)`

Both provider extensions **require** a configuration delegate; there is no parameterless
overload. `RedisOptions.Configuration` is an `Action<ConfigurationOptions>` and
`PostgreSqlOptions.ConnectionString` is a `string`, so neither can be populated with
`Bind` — assign them explicitly.

Do not "fix" the mismatch between namespace root (`Core.`) and package id (`CoreSystem.`).
Consumers depend on the package id; the namespace root is part of the published API surface.

## Architecture rules

1. **The core package is storage-agnostic.** `Core.Idempotency` must never reference
   `StackExchange.Redis`, `Npgsql`, `Dapper`, or any provider package. If core code needs
   storage, extend `IIdempotencyStorage` instead.
2. **Dependency direction is one-way.** Providers → core. Core never references a provider.
   Both providers reference `Core.Idempotency` through a `PackageReference` on the version
   pinned in `Directory.Packages.props`, which must already be on nuget.org. That dependency
   becomes a concrete nuspec dependency and lets each provider ship on its own tag.
3. **Publish order is base-first**: `Core.Idempotency` → `Core.Idempotency.Redis` /
   `Core.Idempotency.PostgreSql`. Publishing a provider before its core puts a package on the
   feed whose dependency does not exist yet.
4. **Extensibility points** are `IIdempotencyStorage`, `IIdempotencyKeyResolver`,
   `IRequestFingerprintProvider`, `IRequestFingerprintBuilder`, `IRequestHasher`.
   Adding a provider means implementing these and shipping a new package — not editing core.
5. **Storage lifecycle is lease-based.** `IIdempotencyStorage` exposes
   `TryAcquireAsync` / `CompleteAsync` / `ReleaseAsync` / `GetAsync` / `SetAsync`.
   Any change to these semantics is a **breaking change** for every provider and must be
   reflected in all providers, tests, docs, and `CHANGELOG.md`.

## Code conventions

Follow the existing style exactly; consistency matters more than personal preference.

- **File-scoped namespaces**, 4-space indent, `var` unless a type is ambiguous.
- **Primary constructors** for implementation classes
  (`internal sealed class IdempotencyService(ILogger<...> logger) : IIdempotencyService`).
  Regular constructors are fine for options/DTOs.
- **Visibility is minimal.** Types are `internal` by default
  (`IdempotencyMiddleware`, `IdempotencyService`, `HeaderIdempotencyKeyResolver`,
  fingerprint internals, both storage implementations, all `*Messages` classes).
  Promote to `public` only what is a deliberate extension or consumer-facing contract
  (`IIdempotencyStorage`, `IIdempotencyKeyResolver`, `IdempotencyOptions`,
  `FingerprintOptions`, `RedisOptions`, `PostgreSqlOptions`, `IdempotencyEntry`,
  `IdempotencyResponse`, `IdempotencyAcquireResult`, `RequestFingerprint`,
  `IdempotencyMetrics`, `IdempotencyFingerprintMismatchException`, the DI extension classes).
- **`InternalsVisibleTo`** is granted to the matching test projects and to
  `DynamicProxyGenAssembly2` (Moq). When a new test project is added, add it to the
  corresponding csproj.
- **XML doc comments** on every public type and member, including `<param>` and `<returns>`.
  Existing options classes are fully documented — keep new ones at that level.
- **Exception and message text is centralized** in `internal static class` message holders
  (`IdempotencyMessages`, `RedisMessages`, `PostgreSqlMessages`). Never inline user-facing
  strings in logic; add them to the corresponding `*Messages` class.
- **Null checks** use `ArgumentNullException.ThrowIfNull(...)`.
- **Configuration** goes in `Options/` classes with safe defaults, a `CopyFrom` method when the
  type is composed (`IdempotencyOptions.CopyFrom`, `FingerprintOptions.CopyFrom`), and
  convenience mutators (`AddAllowedMethods`, `RemoveCacheableStatusCodes`, ...).
  `IdempotencyOptions.Enabled` and provider registration are the master switches: when
  `Enabled` is `false`, `AddCoreIdempotency` returns early and `UseCoreIdempotency` returns
  the app builder untouched. Preserve that no-op behavior.
- **Observability** is built in: any new operation should emit through `IdempotencyMetrics` /
  `IdempotencyDiagnosticsConstants` / the OpenTelemetry contributor, not ad-hoc logs.
- New provider packages must mirror the folder layout of an existing provider:
  `DependencyInjection/`, `Options/`, `Storage/` (plus `Builders/` for Redis key composition).

### PostgreSQL schema

The DDL exists in **two places that must stay in sync**:

- `src/Core.Idempotency.PostgreSql/scripts/postgres.sql` (shipped to consumers)
- `tests/Core.Idempotency.PostgreSql.IntegrationTests/Scripts/postgresql.sql`
  (`CopyToOutputDirectory=PreserveNewest`)

Any column or index change must be applied to both files and covered by an integration test.

## Testing

- Stack: **xunit.v3** (`Fact`/`Theory`), **FluentAssertions**, **Moq**,
  `Microsoft.AspNetCore.TestHost`, **Testcontainers** (Redis, PostgreSql), coverlet.
- Naming: `Method_Should_Do_Expected_When_Condition`, with `// Arrange`, `// Act`,
  `// Assert` comments.
- Test namespaces mirror the source namespace plus the project scope, e.g.
  `Core.Idempotency.UnitTests.DependencyInjection`.
- Unit tests target DI registration and option behavior; **behavior against real stores lives
  in the integration test projects**, which spin up Testcontainers containers. Do not fake a
  Redis/PostgreSQL client in unit tests — extend the integration suites instead.
- Shared provider behavior is expressed in a `IdempotencyTestsBase` per integration project.
  When you add a storage contract case, add it to the shared base so both providers run it.

### Commands

```bash
dotnet build CoreSystem.Idempotency.sln -c Release
dotnet test  CoreSystem.Idempotency.sln -c Release

# individual suites
dotnet test tests/Core.Idempotency.UnitTests/Core.Idempotency.UnitTests.csproj
dotnet test tests/Core.Idempotency.Redis.UnitTests/Core.Idempotency.Redis.UnitTests.csproj
dotnet test tests/Core.Idempotency.PostgreSql.UnitTests/Core.Idempotency.PostgreSql.UnitTests.csproj
dotnet test tests/Core.Idempotency.Redis.IntegrationTests/Core.Idempotency.Redis.IntegrationTests.csproj
dotnet test tests/Core.Idempotency.PostgreSql.IntegrationTests/Core.Idempotency.PostgreSql.IntegrationTests.csproj
```

Integration tests require a working Docker daemon.

## CI/CD

| Workflow | Trigger | What it does |
|---|---|---|
| `.github/workflows/ci.yml` | push/PR to `main` | restore `--locked-mode`, Release build, unit tests, vulnerability audit, then integration tests (Docker) |
| `.github/workflows/codeql.yml` | push/PR to `main`, weekly | CodeQL for `actions` and `csharp` |
| `.github/workflows/documentation.yml` | changes under `docs/**` or `mkdocs.yml`, manual | MkDocs build → GitHub Pages |
| `.github/workflows/publish.yml` | tag `*/v*` | pack + push to nuget.org and GitHub Packages |
| `.github/dependabot.yml` | scheduled | NuGet + GitHub Actions updates |

Because restore is `--locked-mode`, **any package change requires regenerating the lock files**
and committing them:

```bash
dotnet restore CoreSystem.Idempotency.sln --use-lock-file --force-evaluate
git add -- '**/packages.lock.json'
```

Never edit `packages.lock.json` by hand.

## Releasing a package

1. Update `<Version>`, `<AssemblyVersion>`, `<FileVersion>` and `<InformationalVersion>` in the
   package csproj. All four must agree — the nuspec metadata is generated from them.
2. Bump the `CoreSystem.Idempotency` `PackageVersion` in `Directory.Packages.props` **only after
   the new core version is live on nuget.org**, and regenerate the lock files. The providers pin
   the published core package, so a version that does not exist yet fails `--locked-mode` restore
   in CI and at pack time. Add entries for any other external package a project genuinely
   references; never add an entry for a provider, which is only ever referenced by tests.
3. Update `src/Core.Idempotency/CHANGELOG.md` (Keep a Changelog + SemVer). Include a
   `Migration` section with numbered steps for breaking changes.
4. Merge to `main`, then create and push the tag:
   `git tag -a Core.Idempotency/v2.1.0 -m "CoreSystem.Idempotency 2.1.0"` and push it.
   The tag **is** the release; `publish.yml` re-reads the version from it.
5. Watch it: `gh run watch`.

Rules and caveats:

- Tag format is `<project-folder>/v<version>`, e.g. `Core.Idempotency.Redis/v2.1.0`.
- **Published versions are immutable.** A re-run of an already-published version fails with 409;
  `--skip-duplicate` in `publish.yml` does not cover nuget.org's own rejection in all paths.
  Check `https://api.nuget.org/v3-flatcontainer/<package-id>/index.json` before tagging.
- Release is not resumable. A failure on the last tag leaves earlier packages live; recovery is
  manual, so publish the core package first.
- `publish.yml` resolves the project from `src/<project>/<project>.csproj` and needs a `case`
  entry mapping that project to its test projects. **A new package requires a new `case` entry
  there**, otherwise the publish job fails with "No test-project mapping exists".
- `publish-package.bat` and `generate-lock-files.bat` are local helpers (gitignored). Both are
  adapted to `CoreSystem.Idempotency`; edit the `case` block when adding a new provider.
- Note that both `.bat` files are ignored by `.gitignore` (`*.bat`), so they are local
  helpers and never reach other clones.

## Documentation

- Site source lives in `docs/`; navigation is declared in `mkdocs.yml` under `nav`.
  **Adding a page without a `nav` entry means it is not published.**
- Local preview: `serve-docs.bat` (creates `.venv` with Python 3.11, installs `requirements.txt`,
  serves http://127.0.0.1:8000/). `.venv` is gitignored.
- Stack: MkDocs Material + `mkdocs-mermaid2-plugin`. Use ```mermaid fences for diagrams and
  ```text fences for ASCII diagrams.
- `docs/Configuration.md` and `docs/Providers/*.md` must stay in sync with the `Options` classes
  and the `Add*` extension signatures.
- Each package ships its own `README.md` (`PackageReadmeFile`). Update the README of every
  package whose public API or setup steps you change — these are rendered on nuget.org.
- Docs and READMEs use emoji section headings (`# ✨ Features`, `# 📦 ...`), `---` separators,
  and the shields.io badge block at the top. Match that style.

## Git workflow

- `main` is the release branch. **Work on a feature branch and open a PR**; never commit
  directly to `main`.
- One logical change per branch/PR.
- Commit messages: conventional prefix (`feat:`, `fix:`, `docs:`, `chore:`, `refactor:`,
  `test:`), short imperative summary. Keep the subject under ~72 characters.
- **Write everything the repository publishes in English, always.** That means commit
  messages, PR titles, PR descriptions, code comments, XML docs, README files, the docs
  site, and release notes. Never mix another language into them.
- **Talk to the repository owner in Spanish.** Chat replies and explanations may be in
  Spanish; repository artefacts may not. When quoting Spanish conversation into a PR or
  commit, translate rather than paste.
- Never commit `bin/`, `obj/`, `.venv/`, `*.nupkg`, coverage output, or `.env`. See `.gitignore`.
- Never hand-edit workflow YAML indentation — CI failures on a two-space slip are costly.

## Definition of done

Before proposing a change as complete:

1. `dotnet build CoreSystem.Idempotency.sln -c Release` succeeds with no new warnings.
2. All affected test projects pass, including the integration suites when storage behavior changed.
3. Lock files are regenerated and committed if any package reference changed.
4. Public API changes are reflected in the relevant `README.md`, `docs/`, and `CHANGELOG.md`.
5. PostgreSQL schema files are updated in both locations if the schema changed.
6. New public members have XML documentation.