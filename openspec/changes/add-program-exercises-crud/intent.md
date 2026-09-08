# Intent

Anchor: `docs/trainfree-roadmap.md`, slice 8 (`add-program-exercises-crud`), quoted
verbatim below as of the commit that adds this file.

## Frozen roadmap text

8. **`add-program-exercises-crud`** -- Extends admin CRUD with a per-`SessionPhase`
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
