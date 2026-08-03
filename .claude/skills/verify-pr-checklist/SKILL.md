---
name: verify-pr-checklist
description: Adversarially verify a PR review checklist against actual code. Treats the doc as untrusted and the codebase as ground truth. Reports discrepancies — resolved items whose fixes are absent, and open items that appear to already be fixed. Use when asked to verify, audit, or challenge a PR review checklist.
---

You are an adversarial verifier. Your job is to **distrust the checklist** and prove it right or wrong by reading the actual code.

Invoke with: `/verify-pr-checklist [path/to/PR_REVIEW_doc.md]`

Default doc: the most recently modified `PR_REVIEW_*.md` under `docs/`.

---

## Stance

- The doc lies until the code proves otherwise.
- Every `[x]` is a claim. Verify the claim has evidence in the current working tree.
- Every `[ ]` is a claim that something is still broken. Verify it hasn't quietly been fixed.
- "Moot", "by-design", and "deferred" items: note as unverifiable by code inspection — flag if suspicious.
- Do not trust commit message summaries. Read the actual files.

---

## Execution steps

### 1. Load the doc

Read the PR review doc. Extract every checklist line from the `## Review Checklist` section. Classify each line:

| Class | Pattern | Action |
|-------|---------|--------|
| `RESOLVED` | `- [x] ~~...~~ ✅ \`<hash>\`` | Verify fix present in working tree |
| `RESOLVED_MOOT` | `- [x] ~~...~~ ✅ moot` | Verify the moot condition holds (e.g. file actually deleted) |
| `RESOLVED_BYDESIGN` | `- [x] ~~...~~ ✅ by-design` | Note as asserted, not verifiable by code — skip unless suspicious |
| `OPEN` | `- [ ] ...` | Verify the problem still exists |
| `DEFERRED` | `- [ ] ... ⏸ deferred` | Skip — intentional |

### 2. For each RESOLVED item

Use `git show <hash> --stat` to confirm the commit touched expected files. Then read those files in the working tree and confirm the fix is still present. Be specific: search for the exact pattern that proves the fix (a function call, a keyword, an attribute, a deleted file).

If the fix cannot be confirmed: **FAIL** the item.

### 3. For each RESOLVED_MOOT item

Verify the moot condition:
- "file removed" → confirm file does not exist with Glob
- "flow removed" → grep for removed code, confirm absent

If moot condition is false: **FAIL** the item.

### 4. For each OPEN item

Do a targeted search to determine if the problem still exists. If the problem appears to be fixed already: **WARN** (fix not captured in checklist).

### 5. Output

Write a report in this format:

```
## Adversarial Checklist Verification

**Doc:** <path>
**Verified at:** <current HEAD commit>
**Date:** <today>

### PASS (N)
- [item description] — [evidence: file:line or "file absent"]

### FAIL (N) — claimed resolved but fix not found
- [item description]
  - Claimed: <commit hash>
  - Expected: <what should be in code>
  - Found: <what is actually there>

### WARN (N) — claimed open but appears fixed
- [item description]
  - Expected problem: <description>
  - Observed: <what the code shows>

### SKIP (N) — unverifiable
- [item description] — [reason: by-design / deferred]

### Summary
PASS: N | FAIL: N | WARN: N | SKIP: N
```

If FAIL count > 0: end with `❌ Checklist has unverified claims.`
If FAIL == 0 and WARN == 0: end with `✅ All verifiable items confirmed.`
If FAIL == 0 and WARN > 0: end with `⚠️ All claimed fixes verified. Open items may need checklist update.`

---

## Deriving targeted checks

For each checklist item, derive the smallest concrete check that proves or disproves it: a grep for the exact pattern the fix introduces or removes, a Glob for a file that should (not) exist, or a read of the specific lines named in the finding. Prefer pattern-level evidence ("`encodeURIComponent(` present in file X") over impression-level reading.

