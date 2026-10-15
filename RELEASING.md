# Releasing

Commands for cutting a release. The rules and caveats live in `AGENTS.md`; this
file is only the commands.

## Names

| Folder — used in the tag and in paths | PackageId — used on nuget.org |
|---|---|
| `Core.Idempotency` | `CoreSystem.Idempotency` |
| `Core.Idempotency.Redis` | `CoreSystem.Idempotency.Redis` |
| `Core.Idempotency.PostgreSql` | `CoreSystem.Idempotency.PostgreSql` |

The tag carries the **folder** name. `publish.yml` resolves
`src/<folder>/<folder>.csproj` and fails before packing if it does not match.

## Order

Core first, providers after:

```
Core.Idempotency  ->  Core.Idempotency.Redis, Core.Idempotency.PostgreSql
```

A provider pins a `CoreSystem.Idempotency` version that must already be live.

## Pre-flight

```bash
# the version in the csproj must already be the one being released
Select-String -Path "src/Core.Idempotency.Redis/Core.Idempotency.Redis.csproj" `
  -Pattern '<Version>|<AssemblyVersion>|<FileVersion>|<InformationalVersion>'

# the version must not be published yet
curl -s https://api.nuget.org/v3-flatcontainer/coreSystem.idempotency.redis/index.json

# the tag must not exist, locally or on origin
git tag -l "Core.Idempotency.Redis/v2.0.1"
git ls-remote --tags origin "Core.Idempotency.Redis/v2.0.1"

# clean tree, on main
git status --short
git branch --show-current
```

## Tag and push

The tag is the release. `publish.yml` re-reads the version from it.

```bash
git checkout main
git pull origin main

git tag -a Core.Idempotency.Redis/v2.0.1 -m "CoreSystem.Idempotency.Redis 2.0.1"
git push origin Core.Idempotency.Redis/v2.0.1
```

Push only that tag.

## Watch

```bash
gh run list --workflow publish.yml --limit 1
gh run watch
```

Do not poll the nuget.org index — it lags by minutes and the run already reports
whether the push succeeded.

## With the local script

`publish-package.bat` and `generate-lock-files.bat` are gitignored, so they exist
only in this clone. The script runs every pre-flight check above, plus build,
tests, and `.nupkg` verification, and refuses to tag if any of them fail:

```bat
publish-package.bat Core.Idempotency.Redis 2.0.1
generate-lock-files.bat
```

## Two things CI does not check

`publish.yml` verifies the restore, the build, the tests, that `pack` produced one
file with the expected name, and that the version is not already published. It
**never opens the `.nupkg`**. Before tagging, confirm by hand that the package
contains `README.md` and `LICENSE`, that the nuspec declares `<readme>`, and that
every `CoreSystem.Idempotency*` dependency is already published. `publish-package.bat`
does this automatically; doing it by hand does not.

## If it fails

A release is not resumable: `publish.yml` has no `--skip-duplicate`, so re-running
a tag that already published fails with 409. Record which packages went out, and
recover manually. A wrong version is fixed by publishing a new patch — published
versions cannot be replaced.