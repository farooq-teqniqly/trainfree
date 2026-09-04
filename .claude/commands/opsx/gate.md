---
name: "OPSX: Gate"
description: "Fresh-context review gate for the trainfree-lean schema - anchor coverage check plus a cold diff review"
allowed-tools: Bash(openspec:*), Bash(git:*), Bash(gh:*), Agent
category: "Workflow"
tags: ["workflow", "gate", "trainfree-lean", "experimental"]
---

Run the trainfree-lean pilot's review gate (issue #68) after `tasks.md` is complete and
before opening the PR. This replaces the missing review step that `design.md` and
`proposal.md` used to carry indirectly: on a solo repo, this gate is the entire review
function.

**Only applies to changes on the `trainfree-lean` schema.** If `openspec status --change
"<name>" --json` reports a different `schemaName`, stop and tell the user this command is
scoped to the lean-schema pilot.

**Input**: Optionally specify a change name after `/opsx:gate` (e.g., `/opsx:gate
drop-categories-table`). If omitted, infer from conversation context or ask.

**Steps**

1. **Select the change and confirm schema**

   ```bash
   openspec status --change "<name>" --json
   ```

   Confirm `schemaName` is `trainfree-lean`. Note `planningHome`, `changeRoot`, and
   `artifactPaths.specs` from the response.

2. **Find the frozen anchor**

   The anchor is named in the `## Requirement coverage` header of the change's primary
   spec file: a GitHub issue number, or a quoted-into-`intent.md` roadmap slice or
   explore doc (`openspec/changes/<name>/intent.md`). Every anchor type is frozen to a
   commit before step 1 reads it - a live re-fetch (of the issue, of `docs/trainfree-
   roadmap.md`, or of the working tree) is never the source of truth, or a mid-change
   edit to agree with the spec would be silently absorbed instead of flagged. There is
   no freeze path that reads `docs/trainfree-roadmap.md` directly: that file's own
   history spans every slice ever written, so "the oldest commit touching it" is not a
   meaningful freeze point for one change. A roadmap-slice anchor is only ever frozen by
   quoting the slice text into `intent.md`, same as an explore-doc anchor.

   First, confirm the working tree is clean (`git status --porcelain` prints nothing) -
   before staging or committing anything below, not just before step 4's diff. A dirty
   tree here means unrelated staged/unstaged work could get swept into the freeze
   commit.

   - **`intent.md`** (roadmap-slice quote or explore doc): find the freeze commit with
     ```bash
     git log --follow --reverse --format=%H -- "openspec/changes/<name>/intent.md"
     ```
     and take the **first** line - `--reverse` makes that the oldest commit touching the
     file, not the newest. Read the frozen text with `git show <freeze-sha>:<path>`.
   - **GitHub issue**: check whether
     `openspec/changes/<name>/.anchor-snapshot.md` exists and is committed. If not (first
     gate run for this change), create it now:
     ```bash
     gh issue view <n> --json title,body --template '# {{.title}}

     {{.body}}' > "openspec/changes/<name>/.anchor-snapshot.md"
     ```
     Check the command's exit status and that the file is non-empty before going
     further - `gh issue view` can fail (bad issue number, auth, network) while the
     redirect still creates an empty file, and an empty anchor snapshot would freeze
     zero requirements and make step 1 pass vacuously. On failure, stop and report the
     error instead of committing anything. On success:
     ```bash
     git add "openspec/changes/<name>/.anchor-snapshot.md"
     git commit -m "chore(openspec): freeze anchor snapshot for <name> (issue #<n>)" -- "openspec/changes/<name>/.anchor-snapshot.md"
     ```
     Commit with a pathspec (`git commit -- <path>`), not a bare `git commit`, so this
     step can never sweep up other staged changes even if the clean-tree check above
     were somehow bypassed. Then resolve its freeze commit the same way as `intent.md`
     above (same `git log --follow --reverse` command, against `.anchor-snapshot.md`)
     and always read the frozen text via `git show <freeze-sha>:<path>` - never via a
     fresh `gh issue view` call, which would read the issue's current (possibly edited)
     state instead of what was frozen when the gate first ran.

3. **Step 1 - anchor diff (mechanical)**

   Delegate this step to a `haiku`-model subagent (cheap, mechanical - no judgment call
   needed). Give it:
   - The anchor's frozen text, read via `git show <freeze-sha>:<path>` per step 2 - never
     a live `gh issue view` or working-tree read
   - The current `## Requirement coverage` table from the spec
   - The full contents of `tasks.md` (`artifactPaths.tasks` from the step-1 status
     response) - required for the task-coverage check below; do not ask for that check
     without supplying this file

   Ask it to check, and report PASS/FAIL with specifics:
   - Every requirement in the frozen anchor text has a row in the coverage table
     (rule: one row per anchor requirement, covered or not - absence must be visible)
   - No row was fabricated (summarized from the spec instead of the anchor)
   - Every `Not covered` row carries a reason
   - Every `Covered by` row that names a requirement has at least one task in `tasks.md`
     referencing it, or an explicit note why not

   **Deliberate negative test** (run once, when first wiring up this command, not on
   every gate run): delete a row from a test change's coverage index and confirm this
   step reports FAIL. An index that cannot fail is decoration.

4. **Step 2 - cold diff review**

   First, confirm the working tree is clean: `git status --porcelain`. If it prints
   anything, stop and tell the user to commit or stash outstanding changes before
   gating - `git diff main...HEAD` only sees committed history, so an uncommitted
   implementation change would silently bypass this review.

   Spawn a **fresh, non-fork** subagent (`general-purpose` or a code-review-oriented
   agent type) with **no context from this conversation** - it must not inherit any
   rationalization from writing the code. Give it only:
   - The branch diff: `git diff main...HEAD` (or the equivalent base ref)
   - The change's spec file(s) and `tasks.md`, read fresh
   - Instructions to review the diff against the spec's requirements and `## Decisions`
     section, flagging: requirements not actually implemented, implementation that
     contradicts a stated Decision, and correctness/quality issues a normal code review
     would catch

   This step is freshness-by-construction: an inline review or a forked agent shares the
   session's assumptions, which is exactly what a solo-repo pilot has no other way to
   catch.

5. **Report**

   Combine both steps into one PASS/FAIL report:
   - Step 1 (anchor coverage): PASS/FAIL with the specific missing/fabricated/unreasoned
     rows
   - Step 2 (cold review): findings by severity, each with a file:line reference and a
     concrete fix

   If either step fails, do not proceed to PR. Fix the gap, re-run the failed step only
   (re-running step 1 after a spec edit; re-running step 2 after a code fix - a step 2
   fix does not require re-running step 1 unless the coverage table also changed).
