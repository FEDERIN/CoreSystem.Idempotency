# AGENTS.md

Instructions for AI agents (and humans) working in this repository.

## 1. Engineering Standards

Before performing any task, read `CORE-SYSTEM-STANDARDS.md`.

These standards are mandatory and apply to all development
activities in this repository.

The repository-specific instructions in this file complement
the common standards.

If a project-specific rule differs from a common standard,
follow the explicit project-specific exception.

## 2. Project

**CoreSystem.Idempotency** — a production-ready idempotency framework
for ASP.NET Core. It suppresses concurrent duplicate requests, fingerprints
requests, persists successful responses through pluggable storage providers,
and replays them on retry.

Repository: `FEDERIN/CoreSystem.Idempotency` · License: MIT ·
Docs: https://federin.github.io/CoreSystem.Idempotency/

## 3. Environment and build

| Item | Value |
|------|-------|
| .NET SDK | **10.0.201** via `global.json`, `rollForward: latestFeature` |
| Target frameworks | `net8.0;net9.0;net10.0`, declared **only** in the root `Directory.Build.props` |
| Test runner | Microsoft.Testing.Platform (`global.json` `"test"` section) |
| Solution | `CoreSystem.Idempotency.sln` |
| Dependencies | Central Package Management, `CentralPackageTransitivePinningEnabled=true` |
| Lock files | `packages.lock.json` committed per project; restore runs in locked mode |

MSBuild only takes the **nearest** `Directory.Build.props`, so the root one is
imported explicitly by `src/Directory.Build.props` and `tests/Directory.Build.props`.
Do not move these declarations back into csprojs or re-declare inherited
properties (`Nullable`, `ImplicitUsings`) in any props or csproj.

Folder-level props own the deltas:

- `src/`: package metadata, XML docs, SourceLink, `GeneratePackageOnBuild=true`.
- `tests/`: `IsPackable=false`, `GenerateDocumentationFile=false`,
  `NoWarn` for CA1707/CA1001 and (net10.0 only) ASPDEPR004/ASPDEPR008.

`Directory.Build.local.props` (untracked) may set `UseLocalProjectReferences=true`
to point providers at local core projects instead of the published package;
in that mode restore must stay unlocked. The root props handles this.

Commands:

```bash
dotnet build CoreSystem.Idempotency.sln -c Release
dotnet test --solution CoreSystem.Idempotency.sln -c Release

# the MTP runner needs --project, not a bare path
dotnet test --project tests/Core.Idempotency.UnitTests/Core.Idempotency.UnitTests.csproj -c Release
```

Integration suites require a running Docker daemon
(`DockerUnavailableException` otherwise).

Locked restore: any package change requires regenerating the lock files
and committing them:

```bash
dotnet restore CoreSystem.Idempotency.sln --use-lock-file --force-evaluate
git add -- '**/packages.lock.json'
```

Never edit `packages.lock.json` by hand.

## 4. Repository layout and naming

```text
src/
  Core.Idempotency/                     -> package CoreSystem.Idempotency  (core, no storage deps)
  Core.Idempotency.Redis/               -> package CoreSystem.Idempotency.Redis
  Core.Idempotency.PostgreSql/          -> package CoreSystem.Idempotency.PostgreSql
tests/
  Core.Idempotency.UnitTests/           -> core unit tests
  Core.Idempotency.Redis.UnitTests/
  Core.Idempotency.PostgreSql.UnitTests/
  Core.Idempotency.Redis.IntegrationTests/          -> Testcontainers-based
  Core.Idempotency.PostgreSql.IntegrationTests/     -> Testcontainers-based
docs/                                   -> MkDocs Material site
generate-lock-files.bat, publish-package.bat, serve-docs.bat  (all tracked)
RELEASING.md                            -> release command reference
```

Naming rules (they look inconsistent but are intentional; do not "fix" them):

- **Assembly / folder / project**: `Core.Idempotency*`
- **Namespace**: `Core.Idempotency.*`
- **NuGet PackageId**: `CoreSystem.Idempotency*`
- **Public API prefix**: `Core` — `AddCoreIdempotency()`, `UseCoreIdempotency()`,
  `AddCoreIdempotencyRedis(Action<RedisOptions>)`,
  `AddCoreIdempotencyPostgreSql(Action<PostgreSqlOptions>)`.

Both provider extensions **require** a configuration delegate; there is no
parameterless overload. `RedisOptions.Configuration` is an
`Action<ConfigurationOptions>` and `PostgreSqlOptions.ConnectionString` is a
`string`, so neither can be populated with `Bind` — assign them explicitly.
Consumers depend on the package id; the namespace root is published API
surface since 1.x.

## 5. Architecture

- **The core package is storage-agnostic**: `Core.Idempotency` must never
  reference `StackExchange.Redis`, `Npgsql`, `Dapper` or a provider package.
  If core code needs storage, extend `IIdempotencyStorage`.
- **Dependency direction is one-way**: providers → core. Both providers
  consume the core as a **published `PackageReference`** pinned in
  `Directory.Packages.props` — never a `ProjectReference` in the shipped
  configuration (a local override exists for development; see §3).
- **Publish order is base-first**: core → Redis / PostgreSql.
- **Extensibility points**: `IIdempotencyStorage`, `IIdempotencyKeyResolver`,
  `IRequestFingerprintProvider`, `IRequestFingerprintBuilder`, `IRequestHasher`.
  Adding a provider means implementing these and shipping a new package.
- **Storage lifecycle is lease-based** (`TryAcquire` / `Complete` / `Release`
  / `Get` / `Set`). Changing these semantics is a **breaking change** for
  every provider and must be reflected in all providers, tests, docs and
  `CHANGELOG.md`.

## 6. Dependencies

- Versions live only in `Directory.Packages.props`; never use an inline
  `Version` on a `PackageReference`.
- Transitive pins are real constraints, not idle entries. A pin may exist
  with **no direct consumer** because it raises a transitive floor
  (`StackExchange.Redis` ≥ 2.13.17 for `CoreSystem.Redis` — pin it or
  NU1109 blocks restore). Scan the props files too before declaring a pin
  inert, and when an upstream package raises a floor, move the pin in the
  **same commit**.
- The core pin (`CoreSystem.Idempotency`) must move in **its own PR, after
  the new core version is live on nuget.org** — otherwise `--locked-mode`
  restore asks the feed for a version that does not exist and CI deadlocks.
- Keep `<Version>`, `<AssemblyVersion>`, `<FileVersion>` and
  `<InformationalVersion>` in the package csprojs synchronized.

## 7. Code conventions

- **File-scoped namespaces**, 4-space indent, `var` unless ambiguous.
- **Primary constructors** for implementation classes; plain constructors
  for options/DTOs.
- **Minimal visibility**: `internal` by default. Promote to `public` only
  deliberate contracts (the five extensibility points, `IdempotencyOptions`,
  `FingerprintOptions`, both `*Options`, entries/results/fingerprint types,
  `IdempotencyMetrics`, `IdempotencyFingerprintMismatchException`, DI extensions).
- **`InternalsVisibleTo`** to the matching test projects and
  `DynamicProxyGenAssembly2`; add it to the csproj whenever a test project is new.
- **XML docs** on every public type and member, with `<param>`/`<returns>`.
- **Exception text centralized** in `IdempotencyMessages`, `RedisMessages`,
  `PostgreSqlMessages`. Never inline user-facing strings.
- **Null checks** via `ArgumentNullException.ThrowIfNull`.
- **Configuration** in `Options/` classes with safe defaults, `CopyFrom`
  when composed, convenience mutators. `IdempotencyOptions.Enabled=false`
  makes registration a **no-op** (`AddCoreIdempotency` returns early,
  `UseCoreIdempotency` returns the app untouched) — preserve that.
- **Observability** through `IdempotencyMetrics` and the existing
  diagnostics infrastructure, never ad-hoc logs.
- New providers mirror an existing provider's layout:
  `DependencyInjection/`, `Options/`, `Storage/`.

## 8. Testing

- Stack: **xunit.v3**, **FluentAssertions**, **Moq**, TestHost,
  **Testcontainers** (Redis, PostgreSql), coverlet.
- Naming `Method_Should_Do_Expected_When_Condition`; `// Arrange`, `// Act`,
  `// Assert` comments; test namespaces mirror the source namespaces.
- Unit tests cover DI registration and option behavior. **Real store
  behavior lives in the integration suites** — do not fake a Redis or
  PostgreSQL client in unit tests.
- Shared provider behavior is expressed in an `IdempotencyTestsBase` per
  integration project; when a storage contract case is added, add it to
  the shared base so **both** providers run it.

## 9. PostgreSQL schema

The DDL exists in **two synchronized files**:

- `src/Core.Idempotency.PostgreSql/scripts/postgres.sql` (shipped to consumers)
- `tests/Core.Idempotency.PostgreSql.IntegrationTests/Scripts/postgresql.sql`

Any column or index change is applied to both and covered by an
integration test.

## 10. CI/CD

| Workflow | Trigger | What it does |
|---|---|---|
| `ci.yml` | push/PR to `main` | restore `--locked-mode`, Release build, `dotnet format --verify-no-changes`, unit tests, integration tests (Docker) |
| `codeql.yml` | push/PR to `main`, weekly | CodeQL for `actions` and `csharp` |
| `documentation.yml` | `docs/**` or `mkdocs.yml` changes, manual | MkDocs build → GitHub Pages |
| `publish.yml` | tag `*/v*` | pack + push to nuget.org and GitHub Packages |
| `dependabot.yml` | scheduled | NuGet + Actions updates |

There is **no `dotnet list package --vulnerable` step**: the vulnerable-package
gate is the restore itself (`NuGetAudit` + `NuGetAuditMode=all` +
`TreatWarningsAsErrors`), because `dotnet list package --vulnerable` exits 0
even when it reports High advisories.

**A green CI run does not mean the package is correct.** `publish.yml`
verifies restore, build, tests, that pack produced exactly one nupkg with
the expected name, and that the version is not already published. It never
opens the `.nupkg`: README, LICENSE, `<readme>` and nuspec dependency
checks happen only in `publish-package.bat` (tracked, see §11).

CI installs SDKs 8.0.x, 9.0.x and 10.0.x: building and running the
net8.0/net9.0 assets needs their apphost packs and runtimes.

## 11. Release

`RELEASING.md` holds the full command reference; rules here.

1. Update the four version properties in the package csproj (all must agree).
2. Bump the core pin in `Directory.Packages.props` **only after the core
   version is live on nuget.org**, then regenerate lock files (§3, §6).
3. Update `src/Core.Idempotency/CHANGELOG.md` (Keep a Changelog + SemVer;
   `Migration` section with numbered steps for breaking changes).
4. Merge to `main`, then tag: `git tag -a <folder>/v<version>` and push
   only that tag. The tag **is** the release; `publish.yml` re-reads the
   version from it. Watch with `gh run watch`.
5. New packages require a new `case` entry in `publish.yml`'s
   test-project mapping and in `publish-package.bat`, otherwise the
   publish job fails with "No test-project mapping exists".

Rules:

- Tag format: `<project folder>/v<version>`, e.g. `Core.Idempotency.Redis/v2.1.0`.
- **Published versions are immutable.** A duplicate push fails with 409 and
  `publish.yml` deliberately does **not** use `--skip-duplicate`. Check the
  flat-container endpoint (`https://api.nuget.org/v3-flatcontainer/<id>/index.json`)
  before tagging.
- Release is not resumable; publish core first.
- `generate-lock-files.bat`, `publish-package.bat` and `serve-docs.bat`
  are **tracked local helpers** (the `*.bat` ignore rule has explicit
  negations for them). `publish-package.bat` runs every pre-flight gate
  including `.nupkg` verification; treat it as production code.

## 12. Documentation

- Site source in `docs/`, navigation in `mkdocs.yml` under `nav`.
  **Adding a page without a `nav` entry means it is not published.**
- Local preview: `serve-docs.bat` (`.venv` with Python, gitignored), http://127.0.0.1:8000/.
- Stack: MkDocs Material + `mkdocs-mermaid2-plugin`; ```mermaid for
  diagrams, ```text for ASCII.
- `docs/Configuration.md` and `docs/Providers/*.md` must stay in sync
  with the `Options` classes and the `Add*` extension signatures.
- Each package ships its own `README.md` (`PackageReadmeFile`), rendered
  on nuget.org — update it whenever its public API or setup steps change.
- Use the established style: emoji section headings (`# ✨ Features`,
  `# 📦 ...`), `---` separators, shields.io badge block at the top.

## 13. Git workflow

- `main` is protected. **Feature branch + PR**, one logical change per PR,
  never commit directly to `main` — including small fixes.
- Conventional prefix (`feat:`, `fix:`, `docs:`, `chore:`, ...), imperative
  subject under ~72 characters.
- **Repository artifacts are written in English, always**: commits, PRs,
  code comments, XML docs, READMEs, docs, release notes. Chat replies with
  the owner may be in Spanish; translate quotes into artifacts.
- Never commit `bin/`, `obj/`, `.venv/`, `*.nupkg`, coverage output, `.env`.
- Never hand-edit workflow YAML indentation beyond copy-paste level care —
  CI failures on a two-space slip are costly.

## 14. Memory

Read `MEMORY.md` at the beginning of a task.
Update it when significant decisions or project state change.
Keep it concise (max ~50 lines: summarize or drop what no longer
adds value).
If something becomes a permanent rule, propose moving it to
`AGENTS.md` instead of keeping it in memory.
Never store sensitive data (keys, tokens, personal data).

## 15. Definition of done

1. `dotnet build CoreSystem.Idempotency.sln -c Release` succeeds with zero warnings.
2. Affected tests pass, integration suites when storage behavior changed.
3. `dotnet format CoreSystem.Idempotency.sln --verify-no-changes` is clean.
4. Lock files regenerated and committed if any package changed.
5. Public API changes reflected in package `README.md`, `docs/`, `CHANGELOG.md`.
6. PostgreSQL schema updated in both files if changed.
7. New public members have XML documentation.
