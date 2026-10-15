---
name: NuGet Release and Publishing
description: Critical rules for NuGet package immutability, versioning, packaging, and known release risks.
---

# NuGet Release and Publishing

## 1. Immutability and recovery

- Published NuGet versions are immutable; the same version cannot be pushed again.
- If a published package contains an error, fix it and publish a new patch version.
- Check that the version and Git tag do not already exist before publishing.
- A partial release may leave packages or tags already published. Do not blindly rerun successful tags; identify completed steps and recover manually.
- Publish the base package before its providers/dependents, because they may depend on a base-package version that must already be available.

## 2. Consistent versioning

The release version must match across:

- `<Version>`: `1.2.3`
- `<AssemblyVersion>` and `<FileVersion>`: `1.2.3.0`
- `<InformationalVersion>`: `1.2.3`
- The Git tag, using the format defined by the repository.
- The central package version, if the solution pins it explicitly.

Before tagging, validate these versions, update lock files only when appropriate, and run the build, tests, and `dotnet format --verify-no-changes` when required by the repository's release process.

Renaming a public parameter can technically be a breaking change. If it is classified as a patch because it corrects a mismatch with the implemented interface, document the rationale in `<PackageReleaseNotes>`.

## 3. Packaging and verification

A successful build does not guarantee that the `.nupkg` is complete. Inspect the generated package before tagging/publishing and confirm:

- `README.md` is at the package root.
- `LICENSE` is present.
- The `.nuspec` has the correct `PackageId`, version, and metadata.
- Dependencies and packaged content are correct.
- The `.nuspec` declares the packaged README, for example `<readme>README.md</readme>`.

Keep one `README.md` per project and pack it at the package root:

```xml
<PropertyGroup>
  <PackageReadmeFile>README.md</PackageReadmeFile>
</PropertyGroup>
<ItemGroup>
  <None Include="README.md" Pack="true" PackagePath="\" />
</ItemGroup>
```

Centralize shared NuGet configuration in `src/Directory.Build.props` where appropriate. Enable `GenerateDocumentationFile` for production projects, not globally across the solution, to avoid unnecessary CS1591 warnings in test and sample projects.

## 4. Known risks and rules

- Do not rely on `dotnet build` alone; verify the actual `.nupkg` contents.
- Never publish an existing version or overwrite an existing tag.
- Do not run commands that modify lock files without checking their impact. In this repository, `dotnet restore <sln> --use-lock-file --force-evaluate` may change version anchors; the release procedure may prohibit it to avoid unrelated changes.
- `dotnet list package --vulnerable` may return exit code 0 even when it reports a high-severity vulnerability. Do not use it as the sole release gate. Follow the repository's configured audit checks and stop if `restore` fails.
- If a release fails after some packages were published, record exactly which packages were published. Do not assume the entire flow is safe to rerun.
- Repository-specific instructions (`release.md` and its script) define the actual workflow. This skill documents general principles and must not contradict that procedure.
