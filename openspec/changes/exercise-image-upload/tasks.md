## 1. R2 bucket, binding, and migration

- [x] 1.1 Add a one-time setup script `scripts/Create-ExerciseImagesBucket.ps1` (`wrangler r2 bucket create trainfree-exercise-images`, not part of `deploy.yaml`) and run it; verify `wrangler r2 bucket list` shows the bucket.
- [x] 1.2 Add an `r2_buckets` binding named `IMAGES` to `src/Trainfree.AdminApi/wrangler.jsonc` and `wrangler.deploy.jsonc`; verify the existing Worker vitest suite still passes with the binding present (Miniflare provides a real local R2).
- [x] 1.3 Add migration `0017_add_exercises_image_key.sql` adding a nullable `image_key TEXT` column to `exercises`; verify via `migrations.test.js` that it applies on top of 0016 and leaves existing rows with a null key.
- [x] 1.4 Update README's Worker/resource setup section and `docs/trainfree-roadmap.md` slice 14 to note the bucket, the `IMAGES` binding, and that images are served through the Worker; verify by reading both for consistency with the spec's Decisions.

## 2. Worker: image validation (red, then green)

- [x] 2.1 Write failing tests for an image-validation module covering: JPEG and PNG signatures accepted, declared type vs signature mismatch rejected, unsupported declared types (webp, svg) rejected, empty body rejected, exactly 1,048,576 bytes accepted, 1,048,577 rejected; verify they fail for the right reason.
- [x] 2.2 Implement `src/Trainfree.AdminApi/src/image-validation.js` returning a typed result (verified content type, or an error with the HTTP status); verify 2.1 passes.

## 3. Worker: image storage and routes (red, then green)

- [x] 3.1 Write failing tests for `PUT /api/exercises/:id/image`: first upload returns 200 with non-null `imageUrl`; replace changes `imageUrl` and deletes the old R2 object; unknown exercise returns 404 and stores nothing; an exercise referenced by a `program_exercises` row can be uploaded and replaced, and image delete also succeeds for it; validation failures return 413/415/400 and change nothing; verify they fail for the right reason.
- [x] 3.2 Implement upload in `exercises.js` (new random key per upload, R2 put with verified content type, D1 key update, delete old object after the row points at the new key) and route it in `index.js`; verify 3.1 passes.
- [x] 3.3 Write failing tests, then implement `GET /api/exercises/:id/image` (bytes, content type, immutable `Cache-Control`; 404 for unknown exercise or no image); verify tests pass.
- [x] 3.4 Write failing tests, then implement `DELETE /api/exercises/:id/image` (204 and R2 object gone; 404 for unknown exercise or no image); verify tests pass.
- [x] 3.5 Write failing tests, then extend `SELECT_COLUMNS`-based responses so every exercise includes `imageUrl` (null or a relative path that changes per upload) and never the key or bucket name; verify list, create, rename, and upload responses all pass.
- [x] 3.6 Write failing tests, then make `DELETE /api/exercises/:id` delete the R2 object for an unused exercise and leave it intact on the `409` in-use path; verify tests pass.
- [x] 3.7 Write failing tests, then enforce roles: `PUT`/`DELETE` image reject a `User` with `403` and change nothing; `GET` image returns `200` for both `User` and `Administrator` and relays `IdentityApi`'s `401`/`403` without reading R2. Add the `GET` exception to `index.js`'s enforcement (the `isAdministrator` check is skipped for this route only, as with `GET /api/me`); verify tests pass and the other exercise routes still reject `User`.

## 4. Client: domain types and API client

- [x] 4.1 Extend `ExerciseSummary` with `ImageUrl` and deserialize it; verify existing `ExercisesApiClient` tests updated and passing.
- [x] 4.2 Write failing tests, then add `ExerciseImage` state types (`NoImage`, `HasImage`, `StagedImage`) in `Trainfree.Admin`/`Trainfree.Domain` as distinct types with no nullable fields, each with null-guards and XML docs; verify tests pass.
- [x] 4.3 Write failing tests, then add `UploadExerciseImageAsync` and `DeleteExerciseImageAsync` to `IExercisesApiClient`/`ExercisesApiClient` returning outcome types (`UploadExerciseImageSucceeded`/`Failed`, `DeleteExerciseImageSucceeded`/`Failed`) that carry the server's error text; verify every outcome subtype has a test, including 413, 415, 404, and a network failure.

## 5. Client: image processing

- [x] 5.1 Add a JS interop module `wwwroot/js/image-resize.js` that decodes a file, scales the longer edge to at most 800 px without enlarging, and re-encodes in the original format; verify by a test page or bUnit JS-interop stub that a 1600x3200 PNG becomes 400x800 PNG and a 400x700 PNG is unchanged.
- [x] 5.2 Write failing tests, then add a C# `ImageFileValidator` rejecting non-JPG/PNG types (message "Only JPG and PNG are supported") and files over 1,048,576 bytes before any processing; verify tests pass, including exact-limit acceptance.

## 6. Client: Exercises page UI

- [x] 6.1 Write failing bUnit tests for the thumbnail slot: placeholder when `ImageUrl` is null, thumbnail with Replace and image Delete when set, `data-testid` suffixed by row id, accessible names on every control; then add the Image column left of Name; verify tests pass and existing Save/Revert name tests are untouched.
- [x] 6.2 Write failing bUnit tests, then add the staged-preview modal component (contain-fit preview, `Upload`, `Choose different file`, `Cancel`, Escape and outside-click as Cancel, current-vs-new when replacing, controls disabled while uploading, server error shown and controls re-enabled); verify each spec scenario has a passing test and no API call happens before `Upload`.
- [x] 6.3 Wire the page: pick file, validate, resize, stage, upload on confirm, update the row to `HasImage` on success; wire image Delete to the new outcome types, keeping the row thumbnail and showing the error on failure; verify a bUnit test that an unsaved name edit and its Save/Revert are unchanged after an image upload.
- [x] 6.4 Add the thumbnail, modal, and placeholder styles to `app.css` only (Bootstrap utilities first, Bootstrap Icons for glyphs, `aria-hidden` on decorative icons); verify visually with the local dev run below.

## 7. Verification

- [x] 7.1 Run `dotnet test Trainfree.slnx --configuration Release` and the AdminApi `npm test`; verify both pass and `dotnet csharpier check` is clean.
- [x] 7.2 Run Worker and Blazor dev servers, upload, replace, and delete an image from `docs/workouts` on a real row (including a 697x144 strip), and delete an exercise that has an image; verify the thumbnail, preview, and R2 object state at each step with `wrangler r2 object get --local`.
- [x] 7.3 Self-audit the diff against `CLAUDE-baseline.md`: null-guards on new public/internal members, XML docs, one type per file, no unused injected dependencies, explicit `StringComparison`; verify by grepping the diff.
- [x] 7.4 Update `openspec/specs/admin-api-enforcement/spec.md` with the `GET /api/exercises/:id/image` exception (next to `GET /api/me`); verify the wording matches the `exercise-images` role requirement.
- [ ] 7.5 Sync this change's deltas into `openspec/specs/` (`exercise-images` new, `exercises` modified) and delete `openspec/changes/exercise-image-upload/` per the trainfree-lean close-out in README's "OpenSpec changes" section; verify `.github/scripts/require-archived-changes.sh` passes.
