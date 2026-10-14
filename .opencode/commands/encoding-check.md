---
description: Scan for encoding corruption (U+FFFD mojibake, stray BOMs) and fix safely
---

Load the `file-encoding` skill and check text files in this repo for encoding damage.

Scope: $ARGUMENTS

Scan for the replacement character U+FFFD and report affected files. Compare each hit
against git history to determine whether the damage is new or already committed, and
whether it reached a published package. Do not strip BOMs that are part of the
committed state, and never rewrite files with a different encoding than they
already use.