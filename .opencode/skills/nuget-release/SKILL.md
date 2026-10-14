---
name: NuGet Release and Publishing
description: Use when cutting a release, bumping a package version, publishing to NuGet, or setting up tag-driven release automation. Covers the csproj/props/assembly version sync, tag format, verifying the generated .nupkg, and the fact that published versions are immutable.
---

# NuGet Release and Publishing

## Published versions are immutable

NuGet **cannot** re-push an existing version. If a mistake ships (wrong README, a
nonexistent API name in the docs, broken metadata), the only fix is a new patch
version. Plan releases accordingly: verify the artifact *before* tagging.

## Version sync across four places

A release version must match in all of these, or consumers get mismatched metadata:

```xml
<Version>1.2.3</Version>
<AssemblyVersion>1.2.3.0</AssemblyVersion>
<FileVersion>1.2.3.0</FileVersion>
<InformationalVersion>1.2.3</InformationalVersion>
```

Plus the central pin, if the solution references the package:

```xml
<PackageVersion Include="CoreSystem.Foo" Version="1.2.3" />
```

Checklist before tagging:
- [ ] `Version` matches the tag
- [ ] `AssemblyVersion` / `FileVersion` are 4-part
- [ ] `InformationalVersion` is the plain version
- [ ] Central pin bumped (if referenced)
- [ ] Lock files regenerated: `dotnet restore <sln> --use-lock-file --force-evaluate`
- [ ] Build clean, tests green, `dotnet format --verify-no-changes` passes

## Tag-driven automation

Publish via tags, not by pushing to a release branch. Format:
`<src folder name>/v<version>` — e.g. `Core.Cache.Redis/v2.0.3`.

The workflow should:
1. Extract project name and version from the tag (`cut -d'/' -f1` / `-f2`, strip `v`)
2. Validate the folder exists
3. Build that one project with `/p:Version=$VERSION`
4. Run the **full** test suite (not just that project)
5. Push to NuGet.org with `--skip-duplicate`
6. Push to GitHub Packages

## Verify the .nupkg, do not trust the build

A successful build does **not** mean the package is correct. A malformed
`<None Include>` or a wrong `PackageReadmeFile` compiles fine and publishes a
package missing its contents.

After building, inspect the artifact:

```powershell
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($pkg.FullName)
try {
  $zip.Entries | Select-Object FullName
  # and check the nuspec declares <readme>README.md</readme>
} finally { $zip.Dispose() }
```

Confirm: README present at package root, LICENSE present, nuspec metadata correct.

## One README per package

Keep exactly one `README.md` per project and pack it:

```xml
<PropertyGroup>
  <PackageReadmeFile>README.md</PackageReadmeFile>
</PropertyGroup>
<ItemGroup>
  <None Include="README.md" Pack="true" PackagePath="\" />
</ItemGroup>
```

Do not maintain a second `README_NUGET.md` alongside `README.md`. Readers conflate
them, and only one actually ships. The NuGet page renders the packed file, so it is
the source of truth for consumers.

## Semantic versioning judgement

A rename of a public parameter is technically a breaking change, but if the old
name disagreed with the interface being implemented, it is a consistency bugfix and
a **patch** is defensible. Record the reasoning in `<PackageReleaseNotes>` so the
decision is visible to consumers:

```xml
<PackageReleaseNotes>
  2.0.1: SourceLink + XML docs now shipped; renamed ExecuteAsync parameter ct to
  cancellationToken to match IResiliencePipeline; deterministic builds.
</PackageReleaseNotes>
```

## Package metadata is worth centralizing

Put shared NuGet metadata in `src/Directory.Build.props` instead of repeating it in
every csproj:

```xml
<Import Project="$(MSBuildThisFileDirectory)..\Directory.Build.props" />
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
  <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="Microsoft.SourceLink.GitHub" PrivateAssets="all" />
</ItemGroup>
```

Enable `GenerateDocumentationFile` only for `src/`, never repo-wide — it would
produce CS1591 across all test and sample projects.