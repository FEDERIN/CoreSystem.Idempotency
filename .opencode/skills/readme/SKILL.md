---
name: Readme Structure
description: Use when creating or editing a package README.md that ships to a package registry (NuGet/npm/PyPI). Defines the mandatory section layout, badge style, emoji headings and code-block conventions so all package docs in one ecosystem stay consistent.
---

# Package Readme Structure

Every package README that gets published to a registry must follow one structure,
identical across the ecosystem. Pick the most complete existing README in the
repository as the canonical pattern and mirror its section order.

## Layout

1. **Title**: `# ⚡ <PackageId>` — one leading emoji.
2. **Tagline**: a single `> **one-line summary in English.**`
3. **Intro paragraph**: 1–3 sentences on what problem it solves and why it exists.
4. **Badges block** — one badge per line, `style=for-the-badge` so they render
   compactly in Rider:

   ```markdown
   ![NuGet](https://img.shields.io/nuget/v/<PackageId>?style=for-the-badge)
   ![Downloads](https://img.shields.io/nuget/dt/<PackageId>?style=for-the-badge)
   ![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)
   ![.NET](https://img.shields.io/badge/.NET-8.0-blue?style=for-the-badge)
   ```

5. `---` horizontal rule between major sections.
6. **`## ✨ Features`** — bullet list, `- ✅ <feature>` on every item.
7. **`## 📦 Installation`** — install command in a fenced bash block.
8. **`## 🚀 Quick Start`** — minimal runnable example, one short prose line before
   each block so the reader knows what it does.
9. **Concept sections** — one or more `##` covering the package's substance: tables,
   ASCII diagrams in ```text blocks, per-API notes.
10. **`## 🏗 Architecture`** — ASCII diagram, then a short paragraph.
11. **`## 📚 Documentation`** — bullets of what the full docs cover.
12. **`## 🤝 Contributing`** — short.
13. **`## 📄 License`** — `Released under the MIT License.` exactly.

If a section does not apply, keep the heading with a one-line note or drop it
deliberately. Do not invent new top-level sections.

## Conventions

- Content in **English** for registry-facing material.
- No HTML `<p>`/`<br>` in package READMEs (they may appear in a root repo README).
- Fenced code blocks always carry a language tag: ```` ```csharp ````,
  ```` ```bash ````, ```` ```text ````. No space after the backticks:
  ```` ``` csharp ```` renders the same, but breaks the consistency of the
  ecosystem and is rejected in review.
- Emoji vocabulary: `⚡` package → `✨` features → `📦` install → `🚀` quick start →
  `🏗` architecture → `📚` docs → `🤝` contributing → `📄` license → `🏪` components →
  `⚙️` configuration → `✅` feature bullets.
- Headings: exactly one `#`, then `##`, and `###` only inside a section.
- Keep under ~200 lines; push depth into the docs site.

## One README per package

Keep exactly one `README.md` per project and pack that file. Do not maintain a
second `README_NUGET.md` / `README_PACKAGE.md` alongside it — the packed file is the
one consumers see, and two files drift apart. See the `nuget-release` skill for the
csproj wiring.

## Accuracy is part of structure

Before shipping, verify every type name, signature and package name in the README
against the source. A well-structured README that does not compile is still broken.
See the `docs-accuracy` skill.

## Encoding

Write the file as UTF-8 without a BOM. A package README is packed verbatim into
the `.nupkg`, so a corrupted emoji ships to the registry and cannot be corrected
without a new version. See the `file-encoding` skill.

If non-ASCII characters come back as sequences such as `ÔÜí` or `ÔÇö`, the file
passed through a DOS OEM codepage such as CP850 and was re-encoded as UTF-8. That
corruption is lossy for anything outside the Basic Multilingual Plane, so the
emoji cannot be recovered and the file has to be rewritten.

## Adaptation template

Copy the canonical README and replace: title, tagline, intro, package id in badges
and install command, features list, quick-start API names, concept tables,
architecture diagram, docs bullets. Keep the section order identical.