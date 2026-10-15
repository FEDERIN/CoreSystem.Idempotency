# MEMORY

Working state of CoreSystem.Idempotency. Read this first, then `AGENTS.md` for the permanent rules.
Keep it under ~50 lines. If something becomes a standing rule, move it to `AGENTS.md` and drop it here.
Never store secrets, tokens, or personal data.

## Current state (2026-10-14)

- Published on nuget.org: core `CoreSystem.Idempotency` at **2.0.1** (tag
  `Core.Idempotency/v2.0.1`); both providers still at **2.0.0**, whose versions had
  broken docs and a PostgreSQL schema missing `state`/`lease_id`.
- Branch `release/2.0.1-providers` prepares the two provider releases: the providers now
  consume the published core through a pinned `PackageReference` instead of a
  `ProjectReference`. Verified in a detached worktree of the commit: restore
  `--locked-mode`, build with 0 warnings, 3 unit + 2 integration projects green.
- `main` at `9ca13b6` (PR #9, publish workflow). The tag sequence still pending:
  `Core.Idempotency.Redis/v2.0.1` then `Core.Idempotency.PostgreSql/v2.0.1`.
- **2.0.1 is fully released**: core, Redis and PostgreSql all live on nuget.org, tagged
  `Core.Idempotency/v2.0.1`, `Core.Idempotency.Redis/v2.0.1`, `Core.Idempotency.PostgreSql/v2.0.1`.
  `main` at `dcc6ce5` (PR #10).
- Docker Desktop is not started by default here; the integration suites need it and
  fail with `DockerUnavailableException` until it is running.
- **Releases are run by hand with `publish-package.bat`, no agent.** Rationale: the script already
  performs every gate (version not published, no existing tag, clean tree, on main, sync, build,
  tests, `.nupkg` contents, published dependencies), it costs zero tokens, and an agent adds
  ~2.3k tokens of repeated boilerplate per run to re-derive what the script decides mechanically.
  Reserve an agent for the judgement calls: which version to cut, interpreting a failed gate,
  and whether to unlist 2.0.0.
- **`publish.yml` never inspects the `.nupkg`.** It verifies restore `--locked-mode`, build, tests,
  that pack produced exactly one file with the expected name, and gets a 409 on a duplicate
  version. It does not check README, LICENSE, the nuspec `<readme>` declaration or whether the
  pinned dependency exists. So the local script is the only gate against shipping a package like
  2.0.0, whose defects were all invisible to a green CI run. Treat it as production code even
  though it is gitignored.

## Decisions

- **Core carries no storage code.** Storage sits behind `IIdempotencyStorage`, shipped as separate
  packages. Rationale: a consumer picks Redis or PostgreSQL without dragging in both dependency trees.
- **Lease-based contract** (`TryAcquire` / `Complete` / `Release` / `Get` / `Set`) rather than a plain
  get/set cache. Rationale: concurrent duplicates must be *rejected*, not both executed, so acquire has
  to be atomic. Any change here is breaking for every provider.
- **Providers consume the core as a published package, not as a project.** Pinned to 2.0.1
  in `Directory.Packages.props`. Rationale: each provider can then ship on its own tag with a
  concrete nuspec dependency, and the publish workflow restores only the provider csproj.
  Cost: the pinned version must already be live before a provider is tagged, and local
  provider builds test the *published* core rather than the local one.
- **Namespace `Core.Idempotency.*` vs PackageId `CoreSystem.Idempotency*`.** Looks like a bug, is not.
  Rationale: PackageId is the NuGet contract; the namespace is already public API since 1.x.
- **PostgreSQL DDL is duplicated** in `src/.../scripts/postgres.sql` and `tests/.../Scripts/postgresql.sql`.
  Rationale: consumers read the shipped script without referencing the test project. Cost: always edit
  both, never just one.
- **`src/Directory.Build.props` exists and is scoped to `src/`.** It enables XML docs, SourceLink and
  embedded PDBs. Rationale: consumers need IntelliSense; test projects would only collect CS1591.

## Mistakes to avoid

- **Verify the committed state, not the working tree.** This bit twice. A green local build proved
  nothing, because uncommitted lock files made the tree self-consistent while the commit was not.
  Use `git worktree add --detach <temp> HEAD` and run restore and build there before pushing.
- **Lock files are committed and CI restores `--locked-mode`.** Any package change needs
  `dotnet restore --use-lock-file --force-evaluate` *and* the regenerated files staged in the same
  commit. Regenerate from the committed csproj versions, or the lock files will record a version
  that does not exist yet.
- **`git stash pop` aborts entirely on the first conflict** and leaves a partial application. The
  stash is kept, so recover with `git checkout stash@{0} -- <file>` rather than retrying the pop.
- **`Out-File -Encoding utf8` writes a BOM** in Windows PowerShell 5.1, which silently lands in
  commit messages and files. Use `[System.IO.File]::WriteAllText` with `UTF8Encoding($false)`.
- **PowerShell cannot chain `.Replace()` on a new line.** Assign to a variable first.
- **`git stash push --keep-index`** is useful for building only what a PR stages, but note that
  untracked files are *not* stashed, so an untracked props file can enable settings the build
  otherwise would not have.
- **Published versions are immutable.** Check nuget.org before tagging.
- **Do not fake Redis/PostgreSQL in unit tests.** The Testcontainers integration suites are the
  intended place for storage behavior.

## Known gaps

- `publish-package.bat` and `generate-lock-files.bat` are local helpers only: they are gitignored,
  so they never reach another clone and their fixes are invisible to CI. Review them by reading
  them; the repository rules they implement live in `AGENTS.md` and `.opencode/`.
- About 27 inert central package pins remain, including `xunit` 2.5.3 beside the `xunit.v3`
  actually used, and `Microsoft.AspNetCore.Http` 2.3.0.
- Undecided: whether to unlist 2.0.0 on nuget.org, and whether the PostgreSQL provider should
  ever offer opt-in schema creation.