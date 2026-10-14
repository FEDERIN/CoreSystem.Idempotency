---
description: Decide whether a feature should be its own package, and verify a split is clean
---

Load the `package-split-decisions` skill.

Question or change: $ARGUMENTS

First state the split-vs-keep recommendation and the reasoning (does a realistic
consumer need both halves?). If the work is already a completed split, verify that no
compile-time reference, code reference, or stale docs reference to the extracted
feature remains, and that no empty test project was left behind.