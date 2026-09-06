# Trainfree -- Change Roadmap (v0.1)

Captures the incremental, full-stack (UI-to-backend) slicing plan for building Trainfree,
derived from `trainfree-proposal.md`, `screen-mockups.md` (screens 1-10, the not-yet-built
`Trainfree.Workout` app), and `docs/design/admin-mockups/` (the hi-fi design canvas that
superseded screen-mockups.md's screen 11 for the admin app). Each slice below becomes one
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
   slice 8, once there's actual work to put in them. **Done**.
3. **`add-sessions-crud`** -- Extends admin CRUD (now in `Trainfree.Admin`) with the
   `Session` entity (day-sessions under a program, e.g. "Monday Lower Body"). D1 migration:
   `sessions` table (FK to `programs`). Worker: session routes nested or filtered by
   program. Blazor: expand admin UI to session rows. **Done**.
4. **`restyle-admin-shell`** -- No new entities or API routes; rewrites the existing
   `Trainfree.Admin` UI (the plain list from slice 1, extended with session rows by slice 3)
   to match `docs/design/admin-mockups/`. Navbar brand becomes "Trainfree Admin" with a
   dumbbell icon; sidebar nav flattens to `Home` / `Programs` (no `Admin` wrapper --
   `Phases` and `Exercises` links are added in slices 5 and 6 once those pages exist,
   landing in the final order `Home` / `Phases` / `Exercises` / `Programs`); the plain
   unstyled table becomes the bordered, depth-indented spreadsheet look (chevron
   expand/collapse for Program -> Session) with a wide-screen-friendly layout (fixed 240px
   sidebar, body copy capped for readability). The `Home` page also gets its
   `docs/design/admin-mockups/Home.dc.html` treatment: a quick-link tile per library page,
   though only the `Programs` tile is live until slices 5 and 6 add the other two. Depends
   on slice 3 so there's a two-level hierarchy to actually demonstrate the indentation on.
   **Done**.
5. **`add-category-library-crud`** -- Admin CRUD for a canonical `Phase` entity (name
   only -- "Warm Up", "A", "B", ...) per `docs/design/admin-mockups/Phases.dc.html` and
   `PhasesEmpty.dc.html`. D1 migration: `phases` table. Worker:
   `GET/POST/PATCH/DELETE /api/phases` (delete is unconditional -- nothing references a
   phase yet, since slice 7's `SessionPhase` join doesn't exist). Blazor: new `Phases`
   page, added to the sidebar nav between `Home` and the not-yet-built `Exercises` link,
   and a `Phases` tile added to the `Home` page. **Done**; renamed from `Category` to
   `Phase` by
   `rename-category-to-phase` before slice 7 introduced any references to it.
6. **`add-exercise-library-crud`** -- Admin CRUD for a canonical `Exercise` entity (name
   only) per `docs/design/admin-mockups/Exercises.dc.html` and `ExercisesEmpty.dc.html`.
   No `type` (Reps/Timed) field here -- the same exercise can be prescribed either way
   depending on the program (e.g. sit-ups as 3x12 in one program, max reps in 30 seconds
   in another), so `type` is a fact about a program's use of an exercise, not about the
   exercise itself; it lands on slice 7's `ProgramExercise` instead. D1 migration:
   `exercises` table. Worker: `GET/POST/PATCH/DELETE /api/exercises`. Delete is
   unconditional in this slice -- the `ProgramExercise` join that would make an exercise
   "used" doesn't exist until slice 7, so the mockup's disabled-delete/"Used in" state
   isn't real yet; slice 7 adds both the join and the guard together. Blazor: new
   `Exercises` page, landing the sidebar nav in its final order (`Home` / `Phases` /
   `Exercises` / `Programs`) and completing the `Home` page's three tiles. Image upload
   is deferred to slice 13; this slice's page omits the upload affordance entirely rather
   than showing an inert one (the mockup still shows it, matching slice 13's eventual
   state).
7. **`add-program-categories-exercises-crud`** -- Extends admin CRUD with a per-session
   `SessionPhase` join (referencing a `Phase` from slice 5's library) and a
   per-program `ProgramExercise` join referencing an `Exercise` from slice 6's library,
   completing the full spreadsheet per `docs/design/admin-mockups/Main.dc.html`. This is
   where `type` (Reps or Timed) actually lives, since it's a fact about how a program
   prescribes an exercise, not about the exercise itself: a `RepsProgramExercise` (reps,
   weight in lbs as a bare number, sets, restSeconds, side, note) and a
   `TimedProgramExercise` (durationSeconds in place of reps, same remaining fields) are
   distinct types per the DDD "no enum for state that carries different data" rule,
   rather than one `ProgramExercise` with a `Type` enum and nullable reps/duration
   columns side by side. This slice also adds the `Exercise` delete guard deferred from
   slice 6, now that `ProgramExercise` gives "used by a program" a real meaning. A phase
   or exercise row's name is picked from its library via a searchable dropdown (each with
   a "New phase..." / "New exercise..." shortcut into slices 5/6's create flow) instead of
   typed as free text -- the per-row `Image` column from the original mockup 11 is gone,
   since the image now lives once on the canonical `Exercise`. D1 migrations:
   `session_phases`, `program_exercises` tables. Worker: nested routes. Blazor: full
   inline-editable spreadsheet admin UI, collapsible rows, phase- and exercise-picker
   controls. This is the last purely-admin slice -- `Trainfree.Admin` is feature-complete
   for v0.1 after this, and slice 8 begins the workout app.
8. **`add-program-session-select`** -- Client-facing screens 1-2 (Program Select, Session
   Select), built in `Trainfree.Workout`. Read-only against the real API built in slices
   1, 3, 5, 6, 7. No workout execution yet.
9. **`add-workout-runner-untimed`** -- Workout execution for untimed exercises only:
   screens 3 (ready to start), 6 (log set -- untimed), 7 (rest timer). State machine:
   ready -> set-in-progress -> log-set -> rest -> next set/exercise. Writes nothing to
   history yet (that's slice 11).
10. **`add-workout-runner-timed`** -- Extends the runner with timed exercises: screens 4
    (countdown in progress) and 5 (log set -- timed, auto-completes at 0:00 then shows log
    screen before rest). Builds on slice 9's state machine rather than duplicating it.
11. **`add-workout-complete-history-write`** -- Screen 8 (Workout Complete). `END WORKOUT`
    persists the full session (program, day-session, startedAt/endedAt, per-exercise sets
    with actual reps/weight) to D1 via the Worker, then returns to Program Select.
12. **`add-workout-history-view`** -- Screens 9-10 (History List, History Detail).
    Read-only views over the history data written in slice 11.
13. **`add-exercise-images-r2`** -- Wires up the upload control already shown in slice 6's
    `Exercises` page, R2 bucket storage, URL persisted on the `Exercise` record, image
    displayed both there and during the `Trainfree.Workout` runner (screens 3-4). Depends
    on slice 6 (`Exercise` entity must exist) and benefits from slice 9/10 being in place to
    see it rendered live, but is not blocked by 8-12 -- can slot in parallel after slice 6
    if desired.

No further slice for Cloudflare Access -- Access is already configured manually outside
this repo; nothing to build unless that decision changes later.

## Dependency graph

```
1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 9 -> 10 -> 11 -> 12
                       \
                        -> 13 (after 6; independent of 8-12)
```

## Open items deferred to future versions

- Offline-first / local queuing for set logging and history writes during connectivity
  loss.
- Any move of Cloudflare Access configuration from manual dashboard setup into repo IaC.
- A migration dropping the `categories` D1 table, now that `rename-category-to-phase`
  copies its rows into `phases` instead of dropping it outright. Deferred to a later
  slice/PR so the drop happens only once the new Worker serving `/api/phases` is
  confirmed live -- doing create+copy and drop in the same deploy would open a window
  where the still-deploying old Worker 500s on `/api/categories`.
