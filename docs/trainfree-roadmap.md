# Trainfree -- Change Roadmap (v0.1)

Captures the incremental, full-stack (UI-to-backend) slicing plan for building Trainfree,
derived from `trainfree-proposal.md` and `screen-mockups.md`. Each slice below becomes one
OpenSpec change (`openspec/changes/<slug>`), proposed and implemented independently, each
tagged and deployed on completion.

## Architecture decisions (apply to every slice)

See "Project-specific rules" in [`CLAUDE.md`](../CLAUDE.md) -- Worker/D1/R2 topology,
TDD stack, deploy/tag convention, and local dev loop are documented there as the durable
source of truth rather than duplicated here.

## Slices

Each slice is a vertical full-stack cut: Worker route(s) + D1 schema/migration + Blazor UI,
shipped and deployed together. TDD applies within each slice on both stacks.

1. **`add-programs-crud`** -- Admin CRUD for the `Program` entity only (spreadsheet mockup,
   top-level rows: `[+ Program]`, `[x]` delete). D1 migration: `programs` table. Worker:
   `GET/POST/PATCH/DELETE /api/programs`. Blazor: minimal admin page listing/editing
   programs. Smallest possible slice to prove the Worker + D1 + Blazor + deploy pipeline
   end to end.
2. **`add-sessions-crud`** -- Extends admin CRUD with the `Session` entity (day-sessions
   under a program, e.g. "Monday Lower Body"). D1 migration: `sessions` table (FK to
   `programs`). Worker: session routes nested or filtered by program. Blazor: expand admin
   UI to session rows.
3. **`add-categories-exercises-crud`** -- Extends admin CRUD with `Category` (Warm Up, A, B,
   ...) and `Exercise` (name, reps/duration, weight, side, note, restSeconds) entities,
   completing the full spreadsheet (mockup 11). D1 migrations: `categories`, `exercises`
   tables. Worker: nested routes. Blazor: full inline-editable spreadsheet admin UI,
   collapsible rows.
4. **`add-program-session-select`** -- Client-facing screens 1-2 (Program Select, Session
   Select). Read-only against the real API built in slices 1-3. No workout execution yet.
5. **`add-workout-runner-untimed`** -- Workout execution for untimed exercises only:
   screens 3 (ready to start), 6 (log set -- untimed), 7 (rest timer). State machine:
   ready -> set-in-progress -> log-set -> rest -> next set/exercise. Writes nothing to
   history yet (that's slice 7).
6. **`add-workout-runner-timed`** -- Extends the runner with timed exercises: screens 4
   (countdown in progress) and 5 (log set -- timed, auto-completes at 0:00 then shows log
   screen before rest). Builds on slice 5's state machine rather than duplicating it.
7. **`add-workout-complete-history-write`** -- Screen 8 (Workout Complete). `END WORKOUT`
   persists the full session (program, day-session, startedAt/endedAt, per-exercise sets
   with actual reps/weight) to D1 via the Worker, then returns to Program Select.
8. **`add-workout-history-view`** -- Screens 9-10 (History List, History Detail).
   Read-only views over the history data written in slice 7.
9. **`add-exercise-images-r2`** -- `[Brws]` upload control in the admin UI (from mockup 11),
   R2 bucket storage, URL persisted on the `Exercise` record, image displayed during the
   workout runner (screens 3-4). Depends on slice 3 (Exercise entity must exist) and
   benefits from slice 5/6 being in place to see it rendered live, but is not blocked by
   6-8 -- can slot in parallel after slice 3 if desired.

No slice 10 (Cloudflare Access) -- Access is already configured manually outside this
repo; nothing to build unless that decision changes later.

## Dependency graph

```
1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8
                  \
                   -> 9 (after 3; independent of 6-8)
```

## Open items deferred to future versions

- Offline-first / local queuing for set logging and history writes during connectivity
  loss.
- Any move of Cloudflare Access configuration from manual dashboard setup into repo IaC.
