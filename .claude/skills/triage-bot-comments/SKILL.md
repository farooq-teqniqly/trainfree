---
name: triage-bot-comments
description: Triage, fix, and resolve Copilot/Qodo inline review comments on a PR. Fetches all bot threads, classifies each as Fix/Wontfix/Defer, presents a triage table for user approval, applies code/doc fixes, then closes every thread (resolved with reply or explanation). Use when asked to triage bot comments, act on Copilot/Qodo review, or close PR review threads.
---

# triage-bot-comments

Automates the full bot-comment lifecycle: fetch → triage → fix → reply → resolve.

Invoke with: `/triage-bot-comments <PR-number>`

---

## Phase 1 — Fetch

Collect all bot review threads from the PR:

```bash
# All inline review comments
gh api repos/{owner}/{repo}/pulls/{pr}/comments \
  --jq '.[] | {id: .id, user: .user.login, path: .path, line: .line, body: .body}'

# All review-level summaries
gh api repos/{owner}/{repo}/pulls/{pr}/reviews \
  --jq '.[] | {id: .id, user: .user.login, state: .state, body: .body}'
```

Filter to bot authors only: `copilot-pull-request-reviewer[bot]` and `qodo-code-review[bot]`. Ignore human comments — those belong to `/pr-review`.

Then fetch thread node IDs (needed for GraphQL resolve):

```bash
gh api graphql -f query='
{
  repository(owner: "{owner}", name: "{repo}") {
    pullRequest(number: {pr}) {
      reviewThreads(first: 50) {
        nodes {
          id
          isResolved
          comments(first: 1) {
            nodes { databaseId path body }
          }
        }
      }
    }
  }
}'
```

Build a map: `databaseId → threadNodeId`. Skip threads already `isResolved: true`.

---

## Phase 2 — Triage

For each unresolved bot thread, assign one of three verdicts:

| Verdict | When to use |
|---------|-------------|
| **Fix** | Finding is correct; change is small and safe to make now |
| **Wontfix** | Finding is incorrect, by-design, or the comment misunderstands the intent |
| **Defer** | Finding is valid but the fix is a larger scope of work (tracked elsewhere) |

**Triage rules:**

- Typos, wrong titles, broken cref/doc links → always **Fix**
- Comment accuracy disputes where code is intentional → **Wontfix**
- Bugs requiring architectural changes (new service, new table, new OpenSpec change) → **Defer**
- AAA comment style violations per CLAUDE.md → **Fix** if trivial; **Defer** if requires new test structure
- Security/correctness bugs with simple, self-contained fixes → **Fix**
- Security/correctness bugs that touch multiple layers → **Defer** (open a GitHub issue if one does not exist)

Present the triage table to the user before making any changes:

```
| # | Bot | File:Line | Summary | Verdict | Reason |
|---|-----|-----------|---------|---------|--------|
| 1 | Copilot | Foo.cs:5 | broken cref | Fix | ... |
| 2 | Qodo   | Bar.cs:42 | empty id bug | Defer → issue #N | ... |
```

**Wait for user approval.** User may override any verdict. Proceed only after explicit confirmation.

---

## Phase 3 — Apply fixes

For each **Fix** item, apply the change to the working tree using Edit (preferred) or Write. Changes must be minimal — only what the finding requires.

For each **Defer** item that has no tracking issue yet, check with the user whether to open one before resolving.

Do not modify production code for **Wontfix** items.

---

## Phase 4 — Resolve threads

For every thread (Fix, Wontfix, and Defer), close it with an appropriate reply then call the GraphQL resolve mutation.

**Reply pattern by verdict:**

| Verdict | Reply content |
|---------|--------------|
| Fix | "Fixed. [one-line description of what changed]." |
| Wontfix | "Keeping as-is. [one or two sentences explaining why the existing code is correct or intentional]." |
| Defer | "Valid finding. Tracked as issue #N / deferred to [change name]. [one sentence on root cause and why a larger fix is needed]." |

**Post reply:**

```bash
gh api repos/{owner}/{repo}/pulls/{pr}/comments/{comment_id}/replies \
  -X POST -f body="<reply text>"
```

`comment_id` is the `databaseId` of the **first comment** in the thread (the bot's original comment).

**Resolve thread:**

```bash
gh api graphql -f query='
mutation {
  resolveReviewThread(input: {threadId: "<threadNodeId>"}) {
    thread { isResolved }
  }
}'
```

Run all resolves in parallel where there are no dependencies.

---

## Phase 5 — Summary

Output a completion table:

```
| Verdict  | Count | Threads resolved |
|----------|-------|-----------------|
| Fix      | N     | ✅               |
| Wontfix  | N     | ✅               |
| Defer    | N     | ✅               |
| Total    | N     | N               |
```

List any threads that failed to resolve with their node ID and error.

---

## Rules

- **Never resolve without a reply.** Every closed thread must have a human-readable explanation.
- **Never fix without user approval of the triage table.**
- **Skip already-resolved threads** — do not re-open or re-reply.
- **Skip human reviewer comments** — this skill handles bots only.
- **One reply per thread.** If a reply already exists (bot or human), do not add another.
- For multi-comment threads, reply to and resolve using the **first** (root) comment's `databaseId`.
- If the PR number is not provided and there is a current branch with an open PR, detect it via:
  ```bash
  gh pr view --json number --jq '.number'
  ```
