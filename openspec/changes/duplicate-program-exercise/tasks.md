## 1. Duplicate action in the program exercise row

- [x] 1.1 Add a `Duplicate` icon button (`data-testid="program-exercise-duplicate-@programExercise.Id"`) to `Programs.razor`'s program exercise row actions, alongside Save/Revert/Delete, with an accessible name per `CLAUDE-blazor-ui.md`. Verify by inspecting the rendered row in a bUnit test asserting the button exists for a row.
- [x] 1.2 Write a failing bUnit test in `tests/Trainfree.Admin.Tests/Admin/ProgramsPageTests.cs` asserting that clicking `Duplicate` on a `RepsProgramExercise` row calls `CreateRepsProgramExerciseAsync` with that row's `exerciseId`, `reps`, `sets`, and `restSeconds`, then calls `UpdateProgramExerciseAsync` on the returned row's id with the source row's `weight` and `side`. Confirm it fails for the right reason (button/handler does not exist yet).
- [x] 1.3 Implement `DuplicateProgramExerciseAsync(SessionRow session, SessionPhaseRow phase, ProgramExerciseRow row)` in `Programs.razor`'s `@code` block: call the create route matching the row's `IsTimed` (reps vs. duration), then on `CreateProgramExerciseSucceeded` call `UpdateProgramExerciseAsync` with a `ProgramExerciseUpdate` carrying the source row's saved `Weight` and `Side`. Verify 1.2's test passes.
- [x] 1.4 Write a failing bUnit test for the `TimedProgramExercise` variant (mirrors 1.2 for `CreateTimedProgramExerciseAsync`/`durationSeconds`), confirm it fails, then verify it passes against the same implementation.

## 2. New row placement and dirty-state source

- [x] 2.1 Write a failing test asserting the new row is appended to the end of `phase.ProgramExercises` (not inserted next to the source), matching the Decisions in the delta spec. Verify it fails, then passes.
- [x] 2.2 Write a failing test asserting `Duplicate` clones the source row's last-saved values (`SavedCount`/`SavedWeight`/`SavedSets`/`SavedRestSeconds`/`SavedSide`), not its unsaved working values, when the source row is dirty. Verify it fails, then passes.
- [x] 2.3 Write a failing test asserting that when the source row's `weight` is `0` and `side` is `Both` (the create route's own defaults), `Duplicate` still issues the follow-up update call with those values rather than skipping it as a no-op. Verify it fails, then passes.

## 3. Failure paths

- [x] 3.1 Write a failing test asserting that when the create call returns `CreateProgramExerciseFailed`, the source row shows that error, no update call is made, and no new row is appended. Verify it fails, then passes.
- [x] 3.2 Write a failing test asserting that when the create call succeeds but the update call returns `UpdateProgramExerciseFailed` (or throws a caught transport/parse exception), the new row is still appended with the create route's default `weight`/`side` (`0`/`Both`) and shows the update failure's error on the new row, not the source row. Verify it fails, then passes.
- [x] 3.3 Confirm every new `catch` block added for this feature logs at `Warning` or above via `[LoggerMessage]` in `Programs.razor.Logging.cs`, with a new `EventId` that does not collide with an existing one (grep `*.Logging.cs` for `EventId = ` first per `CLAUDE-baseline.md`).

## 4. Verification

- [x] 4.1 Run `dotnet test Trainfree.slnx --configuration Release` and confirm the full suite passes, not just the new tests.
- [x] 4.2 Run `dotnet format`/CSharpier check (or let the pre-commit hook run it) and confirm no formatting diffs remain.
- [x] 4.3 Start the app locally (`dotnet run` for `Trainfree.Admin` against `Trainfree.AdminApi` on port 9999) and manually duplicate a Reps row and a Timed row, confirming the new row appears at the bottom of the phase with matching values, then flip `Side` on the new row and save.
