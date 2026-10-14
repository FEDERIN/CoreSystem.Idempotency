---
name: .NET Quality Gates
description: Use when enabling or troubleshooting compiler/analyzer strictness in a .NET repo — TreatWarningsAsErrors, AnalysisLevel, EnforceCodeStyleInBuild, dotnet format, or when a build fails with CS1591/CS1573/CS1998/CA1305/CA1707/CA1716/CA1725/CA1848. Also use when adding a CI workflow that enforces build + format + test.
---

# .NET Quality Gates

Enabling warnings-as-errors is a **migration**, not a flag. Turning it on before the
warnings are fixed produces hundreds of errors and the gate gets abandoned.

## Required order of operations

1. **Inventory the warnings first.** Never guess:
   ```bash
   dotnet build -c Release --no-incremental 2>&1 | Select-String "warning" |
     ForEach-Object { ($_ -replace '.*warning (\w+).*','$1') } | Group-Object |
     Sort-Object Count -Descending
   ```
   Note: with `GeneratePackageOnBuild=true` each warning is emitted **twice** (build + pack).
   Divide counts by 2 before judging severity.
2. **Fix real defects first** — CA1305 (missing `CultureInfo.InvariantCulture`),
   CA1725 (parameter name disagrees with the interface it implements), CS1998
   (`async` without `await`).
3. **Add genuine documentation** — CS1591/CS1573 mean missing `///` comments on public API.
   Write real summaries, not filler. See the `docs-accuracy` skill.
4. **Suppress only genuine false positives**, inline with a justification:
   ```csharp
   [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords",
       Justification = "'next' matches ASP.NET Core middleware conventions.")]
   ```
5. **Only then enable the gates.**

## Gate configuration

Put these in the root `Directory.Build.props`:

```xml
<PropertyGroup>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  <AnalysisLevel>latest-recommended</AnalysisLevel>
  <Deterministic>true</Deterministic>
</PropertyGroup>
```

`latest-recommended` is the sweet spot. `latest-all` pulls in a large volume of
suggestion-level noise that nobody will ever fix.

## Per-test-project exemptions

Create `tests/Directory.Build.props` that imports the root one, and suppress only
what xUnit conventions force:

```xml
<Project>
  <Import Project="$(MSBuildThisFileDirectory)..\Directory.Build.props" />
  <PropertyGroup>
    <!-- xUnit convention: Method_Scenario naming -->
    <NoWarn>$(NoWarn);CA1707</NoWarn>
  </PropertyGroup>
</Project>
```

CA1707 fires on every `Test_X_When_Y` method name. That is the entire point of the
naming convention — do not rename thousands of tests to satisfy it.

## CI workflow shape

```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '8.0.x'
    cache: true
    cache-dependency-path: '**/packages.lock.json'

- run: dotnet restore <solution>
- run: dotnet build <solution> --configuration Release --no-restore
- run: dotnet format <solution> --verify-no-changes --no-restore
- run: dotnet test <solution> --configuration Release --no-build
```

Put the format check **after** build (it needs the restored graph) and test with
`--no-build` (build already produced the binaries). `--no-restore` everywhere after
the first restore.

## dotnet format is not optional

Run `dotnet format <solution>` once before committing, so CI's
`--verify-no-changes` step stays green. It normalizes whitespace only and produces
a small mechanical diff. It is far cheaper than a formatting surprise in review.

Note `dotnet format` has no `-c/--configuration` flag. It builds Debug by default.
If a project only builds in one configuration, run a Debug build first or the
workspace reports "Required references did not load".

## Gotcha: MSBuildWorkspace and file encoding

`dotnet format` fails with `Required references did not load for X` or
`Invalid character in the given encoding` when a `.csproj` contains invalid byte
sequences. The build itself may still succeed, hiding the problem. If format
mysteriously fails on one project, check that project's encoding — see the
`file-encoding` skill.