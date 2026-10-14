---
name: Package Split and Dependency Decisions
description: Use when deciding whether to extract a feature into its own NuGet package, when reviewing inter-package dependencies, or when a package pulls in heavy transitive dependencies a consumer may not want. Covers split-vs-keep criteria, coupling costs, and decoupling verification.
---

# Package Split and Dependency Decisions

## The test for splitting

A feature belongs in its own package when **a realistic consumer would not install
both**. Concretely, split when the feature:

- Only needs a small subset of the parent package's dependencies
- Is useful in applications that do not want the parent's stack at all
- Has its own lifecycle / versioning cadence

Keep it together when the feature is inseparable from the parent's value proposition.

## Worked example: correlation middleware

A request-correlation middleware (reads or generates `X-Correlation-Id`, enriches
the log scope) lived inside an observability package that pulls in Serilog and
OpenTelemetry. But the middleware only uses `ILogger`, `IOptions` and
`IServiceCollection` — no Serilog, no OTel.

Splitting was right because an API that just wants a correlation header was forced
to take the whole observability stack.

But the split must **not** be papered over with a reverse dependency
(observability → correlation). That would reintroduce exactly the coupling the split
removed, plus a version lock: every correlation patch would force an observability
republish. Leave them independent; users compose both if they want.

## Coupling costs, stated plainly

If you *do* couple package A to package B:

1. Every consumer of A now gets B transitively, and cannot opt out
2. Version bumps become coordinated — B's typo fix forces an A republish
3. Double-registration risk: if a user also calls B's registration extension, you
   need `IsAlreadyRegistered` guards

## Verify the split is actually clean

A split that leaves a reference behind is worse than no split. After extracting:

```powershell
# 1. No compile-time reference remains
Select-String -Path src/A/*.csproj -Pattern "PackageB|ProjectReference"

# 2. No code reference remains (usings, type names, extension calls)
Get-ChildItem -Recurse src/A -Filter *.cs |
  Where-Object { $_.FullName -notmatch '\\obj\\|\\bin\\' } |
  Select-String -Pattern "B\."

# 3. No stale docs reference
Get-ChildItem -Recurse docs -Filter *.md | Select-String -Pattern "B\."
```

Legitimate `using` of *Abstractions* contracts is **not** a leak. Extension-point
interfaces (contributor registries, shared contracts) are meant to be shared. Only
flag references to the extracted feature itself.

## Don't leave an empty test project behind

If extracting a feature moves every test out of a project, delete the project and
remove it from the solution. An empty test project in the solution is misleading —
it implies coverage that does not exist. Replace it with a focused test project for
whatever contracts the package still owns.

## Configuration keys travel with the code

Renaming a config section when you split (e.g. `Core:Observability:Correlation` →
`Core:Correlation`) breaks existing user configuration. That is acceptable for a
package that was **never published**, and wrong for one that was. Check publication
history before changing a section name.

## Verify the packages are consumable as advertised

After a split, confirm the new package builds standalone, packs correctly, and that
its README shows the right install command and namespace. See the `nuget-release`
and `docs-accuracy` skills.