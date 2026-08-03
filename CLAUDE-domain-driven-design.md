# Domain Modeling Principles

These principles apply to any domain model regardless of language or tech stack.
They correct the most common patterns AI code generation tools produce by default.

---

## 1. No Primitive Obsession

Wrap domain concepts in value objects — never use raw strings, ints, decimals, or GUIDs to represent domain values.

**Wrong:** `string currency`, `decimal amount`, `Guid transferId`  
**Right:** `Currency`, `Money`, `TransferId`

Why: Validation lives in one place. The type system enforces correctness at construction.
Primitive values that always travel together (amount + currency) are a **data clump** — collapse them into a single value object (`Money`).

Each currency has its own valid decimal-place count (ISO 4217, for example).
A `Money` type that holds both amount and currency can enforce this invariant at construction — a raw `decimal` cannot.

---

## 2. No Enum for State

State that carries associated data must be a distinct type, not an enum value.

**Wrong:**
```
enum Status { Pending, PartlyApproved, Approved, Executed, Expired, Rejected }
class Transfer {
    Status Status;
    Guid? FirstApprover;   // only meaningful in some states
    Guid? SecondApprover;  // only meaningful in some states
    DateTime? ExecutedAt;  // only meaningful in one state
}
```

**Right:** One class per state. Each class only carries the data relevant to that state
and only exposes operations valid in that state.

```
PendingTransfer      → Approve(), Reject()
PartlyApprovedTransfer → Approve(), Reject()
DualApprovedTransfer → Execute()
AutoApprovedTransfer → Execute()
ExecutedTransfer     → (terminal, no operations)
ExpiredTransfer      → (terminal, no operations)
RejectedTransfer     → (terminal, no operations)
```

---

## 3. No Nullable Fields for Optional Data

If a field is null in some states and non-null in others, that field belongs in a state-specific type — not in a shared base with `?` / `Optional` / nullable annotations.

**Wrong:** `EmployeeId? FirstApprover = null` on a shared `ApprovedTransfer` that covers both auto-approved and dual-approved cases.  
**Right:** `AutoApprovedTransfer` (no approver fields) and `DualApprovedTransfer(EmployeeId FirstApprover, EmployeeId SecondApprover)` (both required, non-nullable).

---

## 4. One Class, One Concept

If a method body contains logic like "if I'm in state X do this, else if state Y do that," the class is secretly multiple classes. Each branch is a question: *am I pretending to be this other class right now?*

Split on those branches. Each resulting class becomes smaller, simpler, and testable in isolation.

---

## 5. Operations Must Not Throw on Valid Calls

An object should never expose a public method that throws because the object is in the wrong state. If `Execute()` can only be called on an approved transfer, `Execute()` must not exist on `PendingTransfer`.

Corollary: no `InvalidOperationException` / `IllegalStateException` as control flow inside domain objects.

---

## 6. Model Outcomes as Types, Not Exceptions

When an operation has multiple possible results (success, expired, duplicate approver, etc.), return a discriminated union / sealed type hierarchy. Force the caller to handle each case at compile time.

**Wrong:** throw an exception when the transfer is already expired  
**Right:** `Execute()` returns `ExecutionOutcome` which is either `Executed` or `Expired` — the caller must pattern-match both

---

## Checklist for AI-generated domain models

Before accepting AI-generated model code, verify:

- [ ] Every domain concept (ID, amount, currency, timestamp) is a named value object
- [ ] No `string`, `int`, `decimal`, `Guid` fields that represent domain concepts
- [ ] No enum used to encode state that has associated data
- [ ] No nullable fields that are only populated in certain states
- [ ] Each state is a distinct type
- [ ] Each type only exposes methods valid in that state
- [ ] Operations return typed outcomes — no exceptions for expected alternate paths
- [ ] No method body with "which state am I in?" branching logic
