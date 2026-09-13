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
   slice 9, once there's actual work to put in them. **Done**.
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
   phase yet, since slice 7's `SessionPhase` join doesn't exist yet). Blazor: new `Phases`
   page, added to the sidebar nav between `Home` and the not-yet-built `Exercises` link,
   and a `Phases` tile added to the `Home` page. **Done**; renamed from `Category` to
   `Phase` by
   `rename-category-to-phase` before slice 7 introduced any references to it.
6. **`add-exercise-library-crud`** -- **Done** (PR #87). Admin CRUD for a canonical
   `Exercise` entity (name only) per `docs/design/admin-mockups/Exercises.dc.html` and
   `ExercisesEmpty.dc.html`.
   No `type` (Reps/Timed) field here -- the same exercise can be prescribed either way
   depending on the program (e.g. sit-ups as 3x12 in one program, max reps in 30 seconds
   in another), so `type` is a fact about a program's use of an exercise, not about the
   exercise itself; it lands on slice 8's `ProgramExercise` instead. D1 migration:
   `exercises` table. Worker: `GET/POST/PATCH/DELETE /api/exercises`. Delete is
   unconditional in this slice -- the `ProgramExercise` join that would make an exercise
   "used" doesn't exist until slice 8, so the mockup's disabled-delete/"Used in" state
   isn't real yet; slice 8 adds both the join and the guard together. Blazor: new
   `Exercises` page, landing the sidebar nav in its final order (`Home` / `Phases` /
   `Exercises` / `Programs`) and completing the `Home` page's three tiles. Image upload
   is deferred to slice 14; this slice's page omits the upload affordance entirely rather
   than showing an inert one (the mockup still shows it, matching slice 14's eventual
   state).
7. **`add-session-phases-crud`** -- **Done** (PR #88). Extends admin CRUD with a per-session `SessionPhase`
   join (referencing a `Phase` from slice 5's library) per
   `docs/design/admin-mockups/Main.dc.html`'s phase rows (no exercises yet -- that's
   slice 8). `SessionPhase` has no name of its own; it only points at a canonical
   `Phase`, so the Worker route supports create/delete but not rename. This slice also
   adds the `Phase` delete guard deferred from slice 5, now that `session_phases` gives
   "used by a session" a real meaning -- a `Phase`/`Exercise` is a global library entity
   (no `user_id` yet, but the guard query is written to generalize once one exists),
   distinct from a `Program`, which is user-scoped; deleting a globally-referenced row
   has to fail rather than cascade. A phase row's name is picked from the `Phases`
   library via a plain dropdown (no search, no inline "New phase..." shortcut -- both
   deferred past this slice) instead of typed as free text. D1 migration:
   `session_phases` table (FK `session_id`, `phase_id`; cascade-deleted when its parent
   `Session` is deleted). Worker: nested routes,
   `GET/POST/DELETE /api/programs/:programId/sessions/:sessionId/phases`. Blazor: session
   rows gain an expand/collapse chevron revealing phase rows beneath them, same
   dirty-row/cascading-delete pattern as session rows under programs.
8. **`add-program-exercises-crud`** -- **Done** (PR #91). Extends admin CRUD with a per-`SessionPhase`
   `ProgramExercise` join referencing an `Exercise` from slice 6's library, completing
   the full spreadsheet per `docs/design/admin-mockups/Main.dc.html`. `ProgramExercise`
   is keyed to a specific `SessionPhase` row (one session's instance of a phase), not to
   the canonical `Phase` -- two sessions that both use the "Warm Up" phase get
   independent exercise lists. This is where `type` (Reps or Timed) actually lives,
   since it's a fact about how a program prescribes an exercise, not about the exercise
   itself: a `RepsProgramExercise` (reps > 0, weight in lbs as a fractional number >= 0,
   sets > 0, restSeconds > 0, side) and a `TimedProgramExercise` (durationSeconds > 0 in
   place of reps, same remaining fields) are distinct types per the DDD "no enum for
   state that carries different data" rule, rather than one `ProgramExercise` with a
   `Type` enum and nullable reps/duration columns side by side -- neither type carries a
   `note` field, deferred past this slice. `side` defaults to `Both` (the other values
   are `Left`/`Right`) on create. A numeric cell (e.g. `Weight`) renders as an en dash
   when its value is `0`; a `TimedProgramExercise` row's `Reps` cell also renders as a
   dash, for the different underlying reason that the type has no `Reps` property at
   all -- the UI must not fake this by carrying a spurious `Reps = 0` on that type. This
   slice also adds the `Exercise` delete guard deferred from slice 6, following the same
   global-entity reasoning slice 7 used for `Phase`. An exercise row's name is picked
   from the `Exercises` library via a plain dropdown (no search, no inline "New
   exercise..." shortcut, matching slice 7's phase picker) instead of typed as free text
   -- the per-row `Image` column from the original mockup 11 is gone, since the image
   now lives once on the canonical `Exercise`. D1 migration: `program_exercises` table
   (FK `session_phase_id`, `exercise_id`, discriminator + type-specific columns;
   cascade-deleted when its parent `SessionPhase` is deleted). Worker: nested routes,
   `.../phases/:sessionPhaseId/exercises`. Blazor: phase rows gain their own
   expand/collapse revealing exercise rows -- full inline-editable spreadsheet, same
   dirty-row pattern as other admin rows. This is the last purely-admin slice --
   `Trainfree.Admin` is feature-complete for v0.1 after this, and slice 9 begins the
   workout app.
8a. **`add-identityapi`** -- New `Trainfree.IdentityApi` Worker: JWT verification, D1
    `logins`/`users`/`roles` schema plus `programs.user_id`, service-binding contract.
    Deployable and testable standalone, no callers wired up yet. See
    `docs/identity/identity-intent-01-identityapi.md`.
8b. **`adminapi-identity-enforcement`** -- Wires every `Trainfree.AdminApi` endpoint to
    call `Trainfree.IdentityApi` and enforce the Administrator role; adds
    `GET /api/me`. Closes the security gap before any UI changes. Depends on 8a. See
    `docs/identity/identity-intent-02-adminapi-enforcement.md`.
8c. **`admin-access-gating`** -- `Trainfree.Admin` startup `/api/me` check and "no
    access" page. Pure UX on top of an already-secure API. Depends on 8b. See
    `docs/identity/identity-intent-03-admin-access-gating.md`.
9. **`add-program-session-select`** -- Client-facing screens 1-2 (Program Select, Session
   Select), built in `Trainfree.Workout`. Read-only against the real API built in slices
   1, 3, 5, 6, 7, 8, plus 8a-8c for the identity infrastructure it depends on. Slice 8b's
   Administrator-only policy is `AdminApi`-specific; this slice (or `Trainfree.WorkoutApi`
   when it exists) must define its own authorization policy that admits a `User`-role
   caller for reads, since a normal user reading their own program/session data through
   the `AdminApi`-style Administrator-only contract would get `403`. See "Open items"
   below -- this policy isn't designed by the identity change and is left for whoever
   builds this slice. No workout execution yet.
10. **`add-workout-runner-untimed`** -- Workout execution for untimed exercises only:
    screens 3 (ready to start), 6 (log set -- untimed), 7 (rest timer). State machine:
    ready -> set-in-progress -> log-set -> rest -> next set/exercise. Writes nothing to
    history yet (that's slice 12).
11. **`add-workout-runner-timed`** -- Extends the runner with timed exercises: screens 4
    (countdown in progress) and 5 (log set -- timed, auto-completes at 0:00 then shows log
    screen before rest). Builds on slice 10's state machine rather than duplicating it.
12. **`add-workout-complete-history-write`** -- Screen 8 (Workout Complete). `END WORKOUT`
    persists the full session (program, day-session, startedAt/endedAt, per-exercise sets
    with actual reps/weight) to D1 via the Worker, then returns to Program Select.
13. **`add-workout-history-view`** -- Screens 9-10 (History List, History Detail).
    Read-only views over the history data written in slice 12.
14. **`add-exercise-images-r2`** -- Wires up the upload control already shown in slice 6's
    `Exercises` page, R2 bucket storage, URL persisted on the `Exercise` record, image
    displayed both there and during the `Trainfree.Workout` runner (screens 3-4). Depends
    on slice 6 (`Exercise` entity must exist) and benefits from slice 10/11 being in place
    to see it rendered live, but is not blocked by 9-13 -- can slot in parallel after
    slice 6 if desired.

Slices 8a-8c add Cloudflare Access-backed authorization (D1 role lookup and enforcement)
on top of the Access authentication already configured manually outside this repo; see
`docs/identity/identity-intent.md` for the full design. No further slice is planned for
Access *configuration* itself (whitelisting emails, the login flow) -- that stays manual.

## Dependency graph

```
1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8 -> 8a -> 8b -> 8c -> 9 -> 10 -> 11 -> 12 -> 13
                       \
                        -> 14 (after 6; independent of 9-13)
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
- An authorization policy for the `Trainfree.Workout`-facing API (slice 9+). Admitting a
  `User`-role caller past a role check is necessary but **not sufficient** on its own:
  today's program/tree queries return every program with no owner filter, and
  `IdentityApi`'s response carries only `email`/`role`, no `user_id` -- so a `User`
  admitted past the role check would read every other user's programs (and, through
  nested queries, their sessions/phases/exercises), not just their own. This isn't
  resolved by this identity change and shouldn't be treated as resolved by merely
  depending on 8a-8c: slice 9 (or `Trainfree.WorkoutApi`) still needs to design (a) how
  a `User`-role caller maps to a `user_id` (`IdentityApi`'s contract likely needs to
  start returning `user_id`, not just `email`/`role`), and (b) owner-scoped filtering on
  every read path a `User` can reach, including nested collections. Slice 8b's
  Administrator-only rule is `AdminApi`'s own policy and doesn't need to change for
  this identity work to ship; this is entirely slice 9's problem to solve before it
  relaxes anything.
