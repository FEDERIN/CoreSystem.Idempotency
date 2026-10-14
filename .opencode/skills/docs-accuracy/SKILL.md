---
name: Documentation Accuracy
description: Use when writing or reviewing README.md files, docs pages, code examples, XML doc comments, or quickstart snippets. Prevents documenting types that do not exist, internal APIs presented as public, wrong package names, and copy-pasted examples that do not compile.
---

# Documentation Accuracy

Documentation that lies is worse than no documentation. A user copies an example,
it does not compile, and they conclude the library is broken.

**This class of error ships to the registry and cannot be corrected** — published
versions are immutable, so fixing it requires a new patch release.

## Always verify type names against source

Never write an example from memory or from an older version. Grep the real
declaration first:

```powershell
Select-String -Path src/**/IWhatever.cs -Pattern "public interface IWhatever"
```

Real defects this catches:
- A README referenced an interface name that never existed (the real contract had a
  different name).
- Docs showed injecting a type that turned out to be `internal`; the public entry
  point was a different interface.
- An install command used the project folder name instead of the `PackageId`, so the
  command simply fails.

## Check accessibility before documenting

A type is only usable by consumers if it is `public`. An `internal` interface in a
public-looking example is a trap. Verify:

```powershell
Select-String -Path src/**/IFoo.cs -Pattern "^\s*(public|internal).*interface IFoo"
```

Same for constructors, extension methods, and namespaces that moved.

## Verify the install command

The package name in `dotnet add package` must equal the `<PackageId>` in the
csproj — not the project folder name, not the class namespace.

## Link package names to their NuGet page

A package name in prose is a dead end: the reader has to go and search for it.
Where a table already lists packages by name, make each one a link to
nuget.org.

| Package | Responsibility |
|----------|----------------|
| [**CoreSystem.Serialization**](https://www.nuget.org/packages/CoreSystem.Serialization) | JSON, MessagePack, and Protocol Buffers |
| [**CoreSystem.Http**](https://www.nuget.org/packages/CoreSystem.Http) | HTTP abstractions |

Scope the rule deliberately, because unbounded linking produces noise:

- **Link inside tables.** A "Companion packages" or "Ecosystem" table is exactly
  the place a reader looks up a package, and one link per row reads as
  deliberate.
- **Do not link every prose mention.** A page that mentions the same package
  eight times should carry one link, not eight. Repeated links on a single page
  read as an editing accident.
- **Do not link inside code blocks.** `dotnet add package CoreSystem.Cache`
  must stay copy-pasteable.
- **Do not link a table column header.** In a comparison table such as
  `| Capability | IDistributedCache | CoreSystem.Cache |`, the name is the
  subject of the comparison, not a reference, and the header is centred.
- Keep the bold markers: `[**Name**](url)`, not `[Name](url)`.

Verify the target exists before linking. `index.json` is the reliable check; a
404 from a flat-container path is not proof the package is missing:

```powershell
Invoke-RestMethod "https://api.nuget.org/v3-flatcontainer/<id-lowercased>/index.json"
```

Link the package page without a version. A versioned link rots on the next
release, and the page's latest-version tab is what the reader wants.

## Verify method signatures, not just names

When a doc calls `AcquireAsync(key, ct)` or `Serialize(value)`, confirm the actual
signature and default values:

```powershell
Select-String -Path src/**/IFoo.cs -Pattern "Task<.*> AcquireAsync" -Context 0,3
```

Signature drift is the most common silent doc bug: the method name survives a
redesign while parameters change.

## Documentation structure

For package READMEs, see the `readme` skill for the mandated section layout. For
accuracy:

- One README per project, named `README.md`, packed via `<PackageReadmeFile>`.
- All content in **English** for registry-facing material.
- Use fenced code blocks with a language tag (```` ```csharp ````, ```` ```bash ````).
- No HTML `<p>`/`<br>` in package READMEs.
- Keep README under ~200 lines; move depth into `docs/`.

## XML doc comments are part of the public API

With `GenerateDocumentationFile=true`, a missing `///` on a public member is a build
error (CS1591) **and** a gap in the consumer's IntelliSense. Write real summaries:

```csharp
/// <summary>
/// Acquires an asynchronous lock associated with the specified key.
/// </summary>
/// <param name="key">Logical identifier of the resource to synchronize.</param>
/// <param name="cancellationToken">Token used to cancel the wait.</param>
/// <returns>A disposable handle that releases the lock when disposed.</returns>
```

Partial `<param>` coverage is CS1573. Either document every parameter or none.

## Docs are only as current as the code

When you rename, move, or change the visibility of a public type, grep the docs for
the old name in the same change:

```powershell
Select-String -Path docs/**/*.md,src/**/*.md -Pattern "OldTypeName"
```

A rename that leaves the docs behind is a regression even though CI is green.

## One H1 per page, and an icon on every H2

Two rules the ecosystem follows on every docs page.

**Exactly one H1.** It is the page title. A second H1 makes the rendered site
emit several `<h1>`, which competes in the page title and breaks the sidebar
position. Sections below the title are `##`, and `###` only where a section
genuinely nests:

```markdown
# 🩺 Health Checks

## 💚 Health States

### ⚠️ Degraded
```

Verify, and check the HTML rather than trusting the markdown:

```powershell
(Get-Content site\HealthChecks\index.html -Raw) -match '<h1'
```

**An icon on every H1, H2 and H3**, semantically consistent across pages. Pick
the vocabulary from the page that already does it completely rather than
inventing one, and reuse it:

| Icon | Use for |
|---|---|
| ⚙️ | configuration, options |
| 🧩 | components, minimal API |
| 💾 / 📖 | store / retrieve |
| 🔄 / 🔁 | lifecycle / rehydration |
| ⚡ | cache-aside |
| 🏷️ | tags, instance name |
| ⏳ / ⏱️ | expiration / interval |
| 📊 | metrics |
| 🩺 | health checks |
| 🔌 | enabling, endpoints |
| 📦 / 🚀 | installation / quick start |
| ✅ / ⚠️ / ❌ | best practices / degraded / failure |
| 🧭 | roadmap, next steps |
| 🔍 | technical assessment |

Two habits worth keeping:

- **Do not re-decorate a heading that already has an icon.** A bulk edit that
  prepends to every `## ` produces doubled icons.
- **Match the separator you are replacing.** A heading may use an em dash
  (U+2014) where you typed an ASCII hyphen; a bulk replace keyed on `## Step 1
  - ...` silently misses all of them. Grep the codepoint before assuming the
  map applied.

## Detecting emoji needs a decoder, not \p{So}

`\p{So}` misses most of the vocabulary. 🏗️ (U+1F3D7) is above the Basic
Multilingual Plane, so it arrives as a surrogate pair (U+D83D U+DED7) and
`[int]$c` never sees it; ⏳ (U+23F3) and ⏱️ (U+23F1) sit outside the usual
symbol blocks entirely. A scan built only on `\p{So}` reports zero icons on a
page that is fully decorated.

Decode the pair, and widen the ranges:

```powershell
function Has-Emoji([string]$s) {
  for ($i = 0; $i -lt $s.Length; $i++) {
    $c = [int]$s[$i]
    if ($c -ge 0xD800 -and $c -le 0xDBFF -and ($i + 1) -lt $s.Length) {
      $lo = [int]$s[$i + 1]
      if ($lo -ge 0xDC00 -and $lo -le 0xDFFF) {
        $full = 0x10000 + (($c - 0xD800) * 0x400) + ($lo - 0xDC00)
        if ($full -ge 0x1F000 -and $full -le 0x1FAFF) { return $true }
        $i++; continue
      }
    }
    if (($c -ge 0x2190 -and $c -le 0x2BFF) -or ($c -ge 0x1F000 -and $c -le 0x1F0FF) -or
        $c -eq 0xFE0F) { return $true }
  }
  return $false
}
```

Confirm a finding by printing the codepoints before acting on it. A count of
zero is far more often a broken detector than a clean page.

## Editing many files: put the script in a file, and give it a BOM

Bulk heading edits go in a `.ps1` file, never inline in the shell. Inline
`-replace` with emoji breaks on quoting, and a script written as UTF-8
**without** a BOM is parsed as ANSI by Windows PowerShell 5.1, which corrupts
every emoji before your logic runs. The failure looks like a parse error
about a stray token, not an encoding error, so it is easy to misdiagnose.

```powershell
# Windows PowerShell 5.1 needs the BOM to read the script as UTF-8.
$t = [System.IO.File]::ReadAllText($p, [System.Text.Encoding]::UTF8)
[System.IO.File]::WriteAllText($p, $t, (New-Object System.Text.UTF8Encoding $true))
```

Keep a per-file map of exact heading to new heading rather than one global
regex: headings repeat across pages (`## Tags`, `## Recovery`, `## Storage
Integration`) and a global rule would rewrite the wrong one. Skip any heading
that already carries an icon, and report how many headings each file changed
so a silent zero is visible.

Verify afterwards that the diff is balanced — the same count on each side —
which proves only heading lines moved:

```powershell
git diff --stat   # e.g. "162 insertions(+), 162 deletions(-)"
```