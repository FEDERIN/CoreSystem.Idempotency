---
description: Prepare work on a protected main (branch, commit, PR)
---

Load the `git-branch-workflow` skill and prepare this change for a pull request.

Scope: $ARGUMENTS

Check current branch and working tree, create an appropriately named branch if the
work is sitting on main, commit with an English message that states the change and
the reasoning, push, and open the PR. Keep the change to one concern. Report the PR
URL when done.