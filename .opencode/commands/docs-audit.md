---
description: Audit documentation for accuracy (types that do not exist, internal APIs, wrong names)
---

Load the `docs-accuracy` skill and audit the documentation in this repo.

Scope: $ARGUMENTS

Check every type name, signature, package id, install command, and code example in
README files and docs pages against the actual source in `src/`. Report each defect
with file, line, and the real declaration that contradicts it. Do not fix anything
until the list is agreed.