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
   end to end. **Done** -- built inside `src/Trainfree.Web`'s `Admin` folder (since renamed
   to `src/Trainfree.Admin` by slice 2), the shared project that predates the admin/workout
   split below.
2. **`split-admin-workout-apps`** -- No new features; restructures the client and its
   Worker to the two-app, two-Worker architecture in CLAUDE.md's Project-specific rules.
   Renames `src/Trainfree.Web` -> `src/Trainfree.Admin` and `src/Trainfree.Api` ->
   `src/Trainfree.AdminApi` (including the deployed Worker's name/URL, `trainfree` ->
   `trainfree-admin`), and extracts what the workout app will also need into shared
   libraries -- `src/Trainfree.Domain` (domain IDs, e.g. `ProgramId`) and
   `src/Trainfree.Versioning` (the deploy-stamp check + its Razor component). Each app
   ends up with its own independent Worker (own assets, own `/api/*`, own D1 binding to
   the shared `trainfree_db` database) rather than one Worker serving both apps' assets
   under different paths -- see the change's `design.md` for why (that section of
   `trainfree-proposal.md` originally sketched a single shared Worker; it now describes
   the two-Worker shape this slice built). `src/Trainfree.Workout` and
   `src/Trainfree.WorkoutApi` are not stubbed out now -- they're built for real in
   slice 5, once there's actual work to put in them. **Done**.
3. **`add-sessions-crud`** -- Extends admin CRUD (now in `Trainfree.Admin`) with the
   `Session` entity (day-sessions under a program, e.g. "Monday Lower Body"). D1 migration:
   `sessions` table (FK to `programs`). Worker: session routes nested or filtered by
   program. Blazor: expand admin UI to session rows.
4. **`add-categories-exercises-crud`** -- Extends admin CRUD with `Category` (Warm Up, A, B,
   ...) and `Exercise` (name, reps/duration, weight, side, note, restSeconds) entities,
   completing the full spreadsheet (mockup 11). D1 migrations: `categories`, `exercises`
   tables. Worker: nested routes. Blazor: full inline-editable spreadsheet admin UI,
   collapsible rows. This is the last purely-admin slice -- `Trainfree.Admin` is feature-
   complete for v0.1 after this, and slice 5 begins the workout app.
5. **`add-program-session-select`** -- Client-facing screens 1-2 (Program Select, Session
   Select), built in `Trainfree.Workout`. Read-only against the real API built in slices
   1, 3, 4. No workout execution yet.
6. **`add-workout-runner-untimed`** -- Workout execution for untimed exercises only:
   screens 3 (ready to start), 6 (log set -- untimed), 7 (rest timer). State machine:
   ready -> set-in-progress -> log-set -> rest -> next set/exercise. Writes nothing to
   history yet (that's slice 8).
7. **`add-workout-runner-timed`** -- Extends the runner with timed exercises: screens 4
   (countdown in progress) and 5 (log set -- timed, auto-completes at 0:00 then shows log
   screen before rest). Builds on slice 6's state machine rather than duplicating it.
8. **`add-workout-complete-history-write`** -- Screen 8 (Workout Complete). `END WORKOUT`
   persists the full session (program, day-session, startedAt/endedAt, per-exercise sets
   with actual reps/weight) to D1 via the Worker, then returns to Program Select.
9. **`add-workout-history-view`** -- Screens 9-10 (History List, History Detail).
   Read-only views over the history data written in slice 8.
10. **`add-exercise-images-r2`** -- `[Brws]` upload control in `Trainfree.Admin` (from
    mockup 11), R2 bucket storage, URL persisted on the `Exercise` record, image displayed
    during the `Trainfree.Workout` runner (screens 3-4). Depends on slice 4 (Exercise
    entity must exist) and benefits from slice 6/7 being in place to see it rendered live,
    but is not blocked by 7-9 -- can slot in parallel after slice 4 if desired.

No further slice for Cloudflare Access -- Access is already configured manually outside
this repo; nothing to build unless that decision changes later.

## Dependency graph

```
1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9
                       \
                        -> 10 (after 4; independent of 7-9)
```

## Open items deferred to future versions

- Offline-first / local queuing for set logging and history writes during connectivity
  loss.
- Any move of Cloudflare Access configuration from manual dashboard setup into repo IaC.
