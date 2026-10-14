---
name: File Encoding Safety
description: Use when writing or bulk-editing files in a repo — especially via PowerShell, scripts, or shell redirection. Prevents silent corruption (mojibake U+FFFD), stray UTF-8 BOMs, and line-ending churn that breaks dotnet format, MSBuild and CI.
---

# File Encoding Safety

Text-file corruption is **silent**. The command succeeds, the build succeeds, and the
damage only surfaces later as an unrelated error ("Invalid character in the given
encoding", "Required references did not load", mojibake on a rendered web page).

## The rule

Always write UTF-8 explicitly, and never assume what the current encoding is.

| What you do | Risk |
|---|---|
| PowerShell `Set-Content` with no `-Encoding` | **Corrupts.** Windows PowerShell 5.1 defaults to the ANSI codepage. Any non-ASCII char becomes mojibake or `U+FFFD`. |
| PowerShell `Set-Content -Encoding utf8` | Writes a **BOM** (5.1). Fine for XML/csproj, but adds noise if the repo is BOM-free. |
| `Out-File` | Same problems as `Set-Content`. |
| Editor tooling (Edit/Write tools) | Safe. Uses the file's existing convention. Prefer these. |
| Bash heredoc / `>` redirect | LF line endings. On Windows this creates whole-file diffs. |

**Prefer the editor tools for file edits.** Reach for shell commands only when the
editor tools cannot express the change (bulk rename, byte-level inspection).

## Safe PowerShell for bulk edits

```powershell
# Read + write explicit UTF-8 without BOM
$t = [System.IO.File]::ReadAllText($path)
[System.IO.File]::WriteAllBytes($path, (New-Object System.Text.UTF8Encoding $false)).GetBytes($t)
```

## Detecting corruption

Scan for the replacement character, which is the fingerprint of a bad transcode:

```powershell
Get-ChildItem -Recurse -File -Include *.cs,*.csproj,*.props,*.md |
  Where-Object { $_.FullName -notmatch '\\obj\\|\\bin\\|\\site\\' } |
  ForEach-Object {
    $b = [System.IO.File]::ReadAllBytes($_.FullName)
    if ([System.Text.Encoding]::UTF8.GetString($b).Contains([char]0xFFFD)) {
      $_.FullName   # corrupted
    }
  }
```

Detect BOMs the same way (first three bytes `EF BB BF`).

## Do NOT "normalize" existing BOMs

A repo developed in Visual Studio typically has UTF-8 **with BOM** on every `.cs`
and `.csproj`, and that is the committed state. Stripping it produces a whole-repo
diff that nobody can review and serves no purpose.

Rule: match whatever the file already does. Do not convert encoding as a side
effect of an unrelated edit.

If you must repair a corrupted file, restore the non-ASCII characters from the
original source (git history, the published package) rather than substituting
ASCII — replacing `✔` with `-` silently changes the rendered output.

## Repairing damage already committed

Corruption often reaches `main` before it is noticed, because CI only catches it
indirectly. When it does:

1. Find the last good version: `git show HEAD~1:path/to/file`.
2. Repair on a branch and publish a patch release — NuGet cannot re-push an existing
   version, so corrupted content already shipped is only fixable by a new version.

## Verify the build actually exercises the file

After an encoding change, run `dotnet format --verify-no-changes`. That step loads
the MSBuild workspace and fails loudly on bad encoding, whereas `dotnet build` may
pass. See the `dotnet-quality-gates` skill.