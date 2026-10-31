# MEMORY

Working state of CoreSystem.Idempotency. Read this first, then `AGENTS.md` for permanent rules.
Keep under ~50 lines. If something becomes a standing rule, move it to `AGENTS.md`. No secrets.

## Current state (2026-10-31, verified)

- All three packages live on nuget.org at **2.1.0** (flat-container checked:
  `CoreSystem.Idempotency`, `.Redis`, `.PostgreSql`), tags `<folder>/v2.1.0` on origin.
  csproj versions also at 2.1.0 and in sync.
- All 19 central pins exist on nuget.org and are current latest stable
  (verified against `v3-flatcontainer/<id>/index.json`, including the three
  TFM-conditional pins). The only pin without a direct consumer is
  `StackExchange.Redis` 2.13.17 — a transitive floor for `CoreSystem.Redis`, not inert.
- Providers consume core via published `PackageReference` (2.1.0 pin). The
  historic ProjectReference inconsistency was resolved; `UseLocalProjectReferences`
  is the untracked-development override if local builds against unreleased core are needed.
- Multi-targeting net8.0;net9.0;net10.0 since 2.1.0. TFM declared only in the root
  `Directory.Build.props`; `src/` and `tests/` props import it and own the deltas.
- `ci.yml` no longer has a `dotnet list package --vulnerable` step; the audit gate is
  restore (`NuGetAuditMode=all` + `TreatWarningsAsErrors`).

## Gotchas (empirically verified in this repo)

- A green local build proves nothing about the commit: uncommitted lock files can make
  the tree self-consistent while the commit is not. Build from `git worktree add --detach`
  on HEAD before pushing.
- Lock files are committed and restore is locked: regenerate with
  `dotnet restore ... --use-lock-file --force-evaluate` from **committed** csproj versions
  and stage them in the same commit as the change.
- A transitive pin is a floor: when an upstream raises its own floor, move the pin in the
  same commit or NU1109 blocks restore (happened with CoreSystem.Redis → StackExchange.Redis).
- The core pin must move in its own PR **after** core is live on nuget.org, or
  `--locked-mode` restore deadlocks against a nonexistent version.
- The checkout moves when a PR merges mid-session: check `git branch --show-current` before
  every commit.
- `git stash pop` aborts on first conflict leaving a partial application; recover with
  `git checkout stash@{0} -- <file>` instead of retrying the pop.
- PowerShell 5.1: `Out-File -Encoding utf8` writes a BOM — use
  `[System.IO.File]::WriteAllText` with `UTF8Encoding($false)`; cannot chain `.Replace()`
  on a fresh line, assign to a variable first.
- `dotnet list package --vulnerable` exits 0 even reporting a High advisory; never a gate.
- Do not fake Redis/PostgreSQL in unit tests; the Testcontainers suites are intended for
  store behavior and require a running Docker daemon.
- Stale `obj/` caches pack content lists: after removing a `<None Pack>` item from a
  csproj, `pack` can keep emitting the old entries (duplicated LICENSE was shipped from a
  clean-looking tree). Delete `obj/` and rebuild before trusting an inspected `.nupkg`.
- PostgreSQL DDL is deliberately duplicated (`src/.../scripts/postgres.sql` shipped vs
  `tests/.../Scripts/postgresql.sql`) — always edit both.
- `publish.yml` never opens the `.nupkg`; only `publish-package.bat` (tracked) checks
  README/LICENSE/`<readme>`/nuspec deps. Treat it as production code.

## Open questions

- Whether to unlist 2.0.0 on nuget.org (defective package, latest is 2.1.0).
- Whether the PostgreSQL provider should ever offer opt-in schema creation.
