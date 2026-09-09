## ADDED Requirements

### Requirement: Admin user can duplicate a program exercise row
Each program exercise row SHALL show a `Duplicate` action alongside `Save`/`Revert`/
`Delete`. Clicking it creates a new program exercise in the same session phase, cloning
the source row's exercise reference, type (`Reps`/`Timed`), reps-or-duration count,
weight, sets, rest seconds, and side. Because the create route does not accept `weight`
or `side`, the page issues a create call with exercise/type/count/sets/rest, then an
update call to set weight and side to match the source row. The new row is appended to
the end of the session phase's program exercise list -- no reorder/position feature
exists, so a duplicate never lands next to its source; the admin user repositions the
new row's field values (most commonly `side`) after duplicating.

**Rationale**: The same exercise is frequently entered multiple times differing only by
`side` (e.g. a left/right dumbbell variant) or repeated as part of a superset-like group.
Duplicating an existing row is faster than re-selecting the exercise and retyping its
prescription from the add-exercise form. Landing at the end rather than beside the source
is an accepted limitation, not a design goal -- see Decisions.

#### Scenario: Duplicating a Reps program exercise
- **WHEN** the admin user clicks `Duplicate` on a `RepsProgramExercise` row
- **THEN** the page calls the create route with that row's `exerciseId`, `type: "Reps"`,
  `reps`, `sets`, and `restSeconds`, then calls the update route on the new row to set
  `weight` and `side` to the source row's values, and appends the new row to the end of
  the phase's exercise list

#### Scenario: Duplicating a Timed program exercise
- **WHEN** the admin user clicks `Duplicate` on a `TimedProgramExercise` row
- **THEN** the page calls the create route with that row's `exerciseId`, `type: "Timed"`,
  `durationSeconds`, `sets`, and `restSeconds`, then calls the update route on the new row
  to set `weight` and `side` to the source row's values, and appends the new row to the
  end of the phase's exercise list

#### Scenario: Source row has default weight and side
- **WHEN** the admin user clicks `Duplicate` on a row whose `weight` is `0` and `side` is
  `Both` (the create route's own defaults)
- **THEN** the page still issues the follow-up update call so the new row's saved state
  matches the source exactly, rather than skipping it as a no-op

#### Scenario: Duplicate's create call fails
- **WHEN** the create call issued by `Duplicate` returns a non-2xx response or a
  transport/parse exception is caught
- **THEN** the page shows the returned error on the source row, issues no update call,
  and adds no new row

#### Scenario: Duplicate's create succeeds but the follow-up update call fails
- **WHEN** the create call succeeds but the follow-up update call returns a non-2xx
  response or a transport/parse exception is caught
- **THEN** the page still appends the new row (created with the create route's default
  `weight` of `0` and `side` of `Both`) and shows the update failure's error on that new
  row, not the source row

#### Scenario: Duplicate is available regardless of the source row's dirty state
- **WHEN** the admin user has unsaved edits on a program exercise row (its `Save`/
  `Revert` buttons are showing) and clicks `Duplicate`
- **THEN** the page duplicates the row's last-saved values, not its unsaved working
  values, and leaves the source row's unsaved edits untouched

## Decisions

- **Duplicate always appends to the end of the phase's exercise list, never inserts next
  to the source row.** No `program_exercises` schema column or backend ordering concept
  exists for row position -- rows list `ORDER BY created_at` (effectively insertion
  order). Inserting next to the source would need a new position/sort column, a
  migration, and reorder-on-insert backend logic, which is a bigger change than a single
  duplicate action. Appending at the end reuses the existing list-ordering behavior
  unchanged. If a future change adds manual drag-reorder for program exercises (e.g. to
  make phase-internal supersets orderable), duplicate-next-to-source becomes cheap to add
  at the same time and should be revisited then.
- **Duplicate needs two API calls (create, then update), not one.** The existing create
  route intentionally omits `weight`/`side` (they default to `0`/`Both`) and no
  create-with-full-fields route exists. Adding one just for duplication would grow the
  API surface for a single client-side convenience feature. Two calls from the client
  reuses `CreateRepsProgramExerciseAsync`/`CreateTimedProgramExerciseAsync` and
  `UpdateProgramExerciseAsync` exactly as the add-exercise and edit-row flows already do.
- **Multi-select duplicate of a group of rows is out of scope for this change.** It needs
  row selection-state UI and a bulk action, which is a materially larger UI change than a
  single per-row button. Split out to a follow-up (issue #108).

## Requirement coverage

Anchor: issue #104 (Implement a Duplicate Exercise functionality)

| # | Anchor requirement | Covered by |
|---|--------------------|-----------|
| 1 | Duplicate a single exercise row that differs only by side, without re-entering it | Req: Admin user can duplicate a program exercise row |
| 2 | Duplicate a repeating group of exercises (multi-select) | Not covered - split into a separate follow-up, issue #108, per Decisions |
