---
name: "OPSX: Gate"
description: "Fresh-context review gate for the trainfree-lean schema - anchor coverage check plus a cold diff review"
allowed-tools: Bash(openspec:*), Bash(git:*), Agent
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
   spec file (a GitHub issue number, a `docs/trainfree-roadmap.md` slice, or
   `openspec/changes/<name>/intent.md`). If the anchor is a roadmap slice or `intent.md`,
   find its freeze commit:

   ```bash
   git log --oneline --follow -- "openspec/changes/<name>/intent.md"
   ```

   (or the equivalent path for a roadmap-slice quote). The **first** commit touching that
   file is the freeze point - the gate diffs the anchor against that commit, not the
   working tree, so an anchor edited mid-change to agree with the spec is visible as a
   diff rather than silently absorbed.

3. **Step 1 - anchor diff (mechanical)**

   Delegate this step to a `haiku`-model subagent (cheap, mechanical - no judgment call
   needed). Give it:
   - The anchor's frozen text (the GitHub issue body via `gh issue view <n>`, or the
     frozen `intent.md`/roadmap-slice text at the freeze commit via
     `git show <freeze-sha>:<path>`)
   - The current `## Requirement coverage` table from the spec

   Ask it to check, and report PASS/FAIL with specifics:
   - Every requirement in the frozen anchor text has a row in the coverage table
     (rule: one row per anchor requirement, covered or not - absence must be visible)
   - No row was fabricated (summarized from the spec instead of the anchor)
   - Every `Not covered` row carries a reason
   - Every `Covered by` row that names a requirement has at least one task in `tasks.md`
     referencing it (verification step) or an explicit note why not

   **Deliberate negative test** (run once, when first wiring up this command, not on
   every gate run): delete a row from a test change's coverage index and confirm this
   step reports FAIL. An index that cannot fail is decoration.

4. **Step 2 - cold diff review**

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
