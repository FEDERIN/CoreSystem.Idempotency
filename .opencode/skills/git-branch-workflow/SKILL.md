---
name: Git Branch and PR Workflow
description: Use when starting work, committing, or preparing a release on a repo with protected main, or when a git push fails with GH006 or an internal server error. Covers branch-per-change, PR creation, and release tagging without polluting main.
---

# Git Branch and PR Workflow

## main is protected

When `git push origin main` returns:

```
remote: error: GH006: Protected branch update failed for refs/heads/main.
remote: - Changes must be made through a pull request.
```

You are on the wrong branch. Move the commit:

```powershell
git branch <new-branch>        # label current HEAD
git checkout main
git reset --hard origin/main   # discard the accidental local commit
git checkout <new-branch>      # commit comes along
git push -u origin <new-branch>
```

Then open the PR:

```powershell
gh pr create --base main --head <new-branch> --title "..." --body "..."
```

Check state before assuming: `gh pr view <n> --json state,mergedAt`.

## Branch naming

`chore/`, `fix/`, `feat/`, `docs/`, `refactor/`, `test/` prefixes. Name the branch
after the intent, not the tool.

## One concern per PR

Mixing a refactor, a version bump, and new docs in one PR makes review unreliable
and bisecting painful. Split them. A version bump for a release that only carries
doc fixes is a separate commit from the refactor that motivated it.

## Release tags come from a merged state

Prefer tagging **after** the PR merges so the tag points at merged `main`. If you
must tag from a feature branch to save time, verify the full test suite passes on
that exact commit — CI on the PR already proves it.

```powershell
git tag Core.Cache.Redis/v2.0.3
git push origin Core.Cache.Redis/v2.0.3
```

## Transient push failures

`! [remote rejected] <branch> (Internal Server Error)` with no `GH006` is a
transient server-side failure, not a permissions problem. Confirm by checking that
reads still work (`git ls-remote origin`), wait, and retry. Renaming the branch does
not help; retrying does.

## Cleaning up branches

Delete only branches whose tip is an ancestor of `main`:

```powershell
git merge-base --is-ancestor <branch> main   # exit 0 = safe to delete
git branch -d <branch>
```

`git branch -d` refuses to delete unmerged branches. If it blocks a branch that *is*
merged but whose upstream is stale, verify explicitly:

```powershell
git rev-list --count main..<branch>   # must be 0
git branch -D <branch>
```

If a branch has commits not in `main`, check whether that work was migrated
elsewhere (extracted to another repo, superseded) before deleting. Recoverable
history is cheap to keep; lost work is not.

Also prune remote branches deleted elsewhere:

```powershell
git fetch origin --prune
```

## Commit messages in English

Describe the change and, when non-obvious, the reasoning. Reference the finding, not
just the fix. Example:

```
chore: bump Memory 1.0.3, Redis 1.0.3 (corrected READMEs)
```