# exercise-images Specification

## Purpose
Lets the admin attach one picture to each exercise so the workout app can show it
during a workout. Covers the Worker's image upload/serve/delete API, R2 storage, and
the admin UI's staged-preview upload flow.

## Requirements
### Requirement: Upload or replace an exercise image
The system SHALL provide `PUT /api/exercises/:id/image` with the raw image bytes as the
request body and a `Content-Type` of `image/jpeg` or `image/png`. The Worker SHALL store
the bytes in R2 under a newly generated key, record that key on the exercise, and
respond `200` with the updated exercise. When the exercise already had an image, the
Worker SHALL delete the previous R2 object after the new key is recorded.
**Rationale**: A fresh key per upload changes the image URL, so browsers and the edge
never serve a stale copy after a replace. Deleting the old object only after the row
points at the new one means a failed upload never leaves an exercise without an image.

#### Scenario: First upload
- **WHEN** a client calls `PUT /api/exercises/:id/image` with a valid PNG body for an
  exercise that has no image
- **THEN** the Worker stores the object, records its key on the exercise, and responds
  `200` with the exercise including a non-null `imageUrl`

#### Scenario: Replace changes the image URL
- **WHEN** a client uploads a valid image for an exercise that already has one
- **THEN** the response's `imageUrl` differs from the previous `imageUrl`, and the
  previous R2 object no longer exists

#### Scenario: Exercise does not exist
- **WHEN** a client calls `PUT /api/exercises/:id/image` for an `:id` with no matching
  exercise
- **THEN** the Worker responds `404` and stores nothing

#### Scenario: Exercise is used by a program exercise
- **WHEN** a client uploads or replaces the image of an exercise referenced by at least
  one `program_exercises` row
- **THEN** the Worker accepts it exactly as for an unused exercise, and the program
  exercise rows are unchanged

### Requirement: Accepted image types and size
The Worker SHALL accept only JPEG and PNG images of at most 1 MB (1,048,576 bytes). It
SHALL verify the file signature of the body (JPEG `FF D8 FF`; PNG
`89 50 4E 47 0D 0A 1A 0A`) and that it agrees with the declared `Content-Type`,
rejecting any mismatch. The Worker SHALL store the object with the verified content type.
**Rationale**: The client can declare any type, so the Worker is the enforcement point;
the client-side check exists only for fast feedback. SVG is excluded because it can carry
script.

#### Scenario: Body over the size limit
- **WHEN** a client uploads a body larger than 1,048,576 bytes
- **THEN** the Worker responds `413` with a JSON error body and changes nothing

#### Scenario: Body at the size limit
- **WHEN** a client uploads a valid PNG of exactly 1,048,576 bytes
- **THEN** the Worker accepts it

#### Scenario: Unsupported declared type
- **WHEN** a client uploads with `Content-Type: image/webp` or `image/svg+xml`
- **THEN** the Worker responds `415` with a JSON error body and changes nothing

#### Scenario: Signature does not match declared type
- **WHEN** a client uploads a body whose bytes are not a JPEG or PNG signature, or are
  a PNG while declaring `image/jpeg`
- **THEN** the Worker responds `415` with a JSON error body and changes nothing

#### Scenario: Empty body
- **WHEN** a client uploads an empty body
- **THEN** the Worker responds `400` with a JSON error body and changes nothing

### Requirement: Serve an exercise image
The system SHALL provide `GET /api/exercises/:id/image`, responding with the stored
bytes and their content type, and a long-lived immutable `Cache-Control`. The response
SHALL be `404` when the exercise does not exist or has no image.
**Rationale**: The bucket is not public; serving through the Worker keeps the image
behind the same Cloudflare Access gate as the rest of the API. Long caching is safe
because a replace changes the URL (see upload requirement).

#### Scenario: Exercise has an image
- **WHEN** a client calls `GET /api/exercises/:id/image` for an exercise with an image
- **THEN** the Worker responds `200` with the stored bytes, the stored content type, and
  an immutable cache header

#### Scenario: Exercise has no image
- **WHEN** a client calls `GET /api/exercises/:id/image` for an exercise with no image
- **THEN** the Worker responds `404`

### Requirement: Delete an exercise image
The system SHALL provide `DELETE /api/exercises/:id/image`, which clears the image key
on the exercise and deletes the R2 object, responding `204`. It SHALL respond `404` when
the exercise does not exist or has no image.

#### Scenario: Delete an existing image
- **WHEN** a client calls `DELETE /api/exercises/:id/image` for an exercise with an image
- **THEN** the Worker responds `204`, the exercise's `imageUrl` is null on the next
  `GET /api/exercises`, and the R2 object no longer exists

#### Scenario: Exercise has no image
- **WHEN** a client calls `DELETE /api/exercises/:id/image` for an exercise with no image
- **THEN** the Worker responds `404`

#### Scenario: Exercise is used by a program exercise
- **WHEN** a client deletes the image of an exercise referenced by at least one
  `program_exercises` row
- **THEN** the Worker responds `204`; the usage guard on deleting an exercise does not
  apply to its image

### Requirement: Exercise responses expose the image URL
Every exercise returned by the API SHALL include `imageUrl`: a same-origin relative path
to the image endpoint whose value changes on every upload, or `null` when the exercise
has no image. The API SHALL NOT expose the R2 key or bucket name.

#### Scenario: Exercise with an image in the list
- **WHEN** a client calls `GET /api/exercises` and an exercise has an image
- **THEN** that exercise's `imageUrl` is a non-null relative path that, when requested,
  returns the image

#### Scenario: Exercise without an image in the list
- **WHEN** a client calls `GET /api/exercises` and an exercise has no image
- **THEN** that exercise's `imageUrl` is `null`

### Requirement: Image access by role
The upload and delete image endpoints SHALL require the Administrator role, enforced
like the other `/api/exercises` writes. `GET /api/exercises/:id/image` SHALL be allowed
for any provisioned identity, Administrator or User. It SHALL still call `IdentityApi`
first and relay `IdentityApi`'s own `401` and `403` verbatim.
**Rationale**: The workout app is used by the `User` role and shows these images, so
blocking `User` from reading them would break that app. Reading an image exposes nothing
a `User` cannot already see in the workout, while changing or removing one stays an
admin action. This makes the serve route the one deliberate exception to the
Administrator-only rule in `admin-api-enforcement`, alongside `GET /api/me`.

#### Scenario: Non-administrator uploads
- **WHEN** a caller with the `User` role calls `PUT /api/exercises/:id/image`
- **THEN** the Worker responds `403` and stores nothing

#### Scenario: Non-administrator deletes
- **WHEN** a caller with the `User` role calls `DELETE /api/exercises/:id/image`
- **THEN** the Worker responds `403` and changes nothing

#### Scenario: User reads an image
- **WHEN** a caller with the `User` role calls `GET /api/exercises/:id/image` for an
  exercise with an image
- **THEN** the Worker responds `200` with the image bytes

#### Scenario: Administrator reads an image
- **WHEN** a caller with the `Administrator` role calls `GET /api/exercises/:id/image`
- **THEN** the Worker responds `200` with the image bytes

#### Scenario: Unauthenticated or unprovisioned caller reads an image
- **WHEN** `IdentityApi` responds `401` or `403` for a `GET /api/exercises/:id/image`
  request
- **THEN** the Worker relays that status and does not read R2

### Requirement: Staged-preview upload flow
On the Exercises page, choosing a file SHALL NOT upload it. The page SHALL validate the
file's type (JPG or PNG) and size (at most 1 MB) immediately, downscale it on the client
so its longer edge is at most 800 px while keeping its original format, and open a modal
showing a large preview of the processed result inside a contain-fit box. The modal SHALL
offer `Upload`, `Choose different file`, and `Cancel`. `Upload` SHALL call the upload
endpoint; `Cancel`, Escape, or clicking outside the modal SHALL discard the staged file
with no API call. When the exercise already has an image, the modal SHALL show the
current image beside the new one.
**Rationale**: The image is shared data the workout app will display, and a replace
costs an R2 write and a new URL, so one confirming step is worth the extra click. The
preview shows the processed bytes so the admin sees what will actually be stored.
Downscaling never enlarges an image already within 800 px and never changes PNG to JPEG,
because the source images are crisp illustrations where JPEG would add artifacts.

#### Scenario: Picking a valid file opens the preview
- **WHEN** the admin picks a 300 KB PNG for an exercise
- **THEN** the modal opens with a preview and no request is sent to the Worker

#### Scenario: Unsupported file type is rejected at pick time
- **WHEN** the admin picks a `.gif` or `.webp` file
- **THEN** the page shows "Only JPG and PNG are supported" on that row, opens no modal,
  and sends no request

#### Scenario: File over the size limit is rejected at pick time
- **WHEN** the admin picks a file larger than 1 MB
- **THEN** the page shows a size error on that row, opens no modal, and sends no request

#### Scenario: Large image is downscaled
- **WHEN** the admin picks a 1600x3200 PNG under the size limit
- **THEN** the uploaded image is at most 800 px on its longer edge, is still a PNG, and
  keeps its aspect ratio

#### Scenario: Small image is not enlarged
- **WHEN** the admin picks a 400x700 PNG
- **THEN** the uploaded image keeps its 400x700 dimensions

#### Scenario: Confirming the upload
- **WHEN** the admin clicks `Upload` in the modal
- **THEN** the page disables the modal controls, calls `PUT /api/exercises/:id/image`,
  and on success closes the modal and shows the new thumbnail on that row

#### Scenario: Choosing a different file
- **WHEN** the admin clicks `Choose different file` and picks another valid file
- **THEN** the modal's preview is replaced by the new file and nothing is uploaded

#### Scenario: Cancelling
- **WHEN** the admin clicks `Cancel`, presses Escape, or clicks outside the modal
- **THEN** the staged file is discarded, the row shows what it showed before, and no API
  call is made

#### Scenario: Replace shows current and new
- **WHEN** the admin stages a file for an exercise that already has an image
- **THEN** the modal shows the current image and the new image side by side

#### Scenario: Upload is rejected by the Worker
- **WHEN** the upload call returns an error status
- **THEN** the modal stays open, shows the returned error, and re-enables its controls

### Requirement: Image thumbnail column
Each exercise row SHALL show a fixed-size square image slot left of the name. With no
image it SHALL show a placeholder that opens the file picker when activated. With an
image it SHALL show the thumbnail, a `Replace` action that opens the file picker, a
`Delete` image action, and open a larger preview when the thumbnail is activated.
Deleting an image SHALL NOT delete the exercise, and SHALL call
`DELETE /api/exercises/:id/image`. Image state is independent of the name's
Save/Revert state.
**Rationale**: A fixed-size slot keeps rows from changing height when an image is added
or removed. Keeping image actions visually apart from the row's exercise `Delete` avoids
deleting the wrong thing.

#### Scenario: Row without an image
- **WHEN** the page renders an exercise with `imageUrl` null
- **THEN** the row shows the placeholder slot, no `Replace`, and no image `Delete`

#### Scenario: Row with an image
- **WHEN** the page renders an exercise with a non-null `imageUrl`
- **THEN** the row shows the thumbnail with `Replace` and image `Delete` actions

#### Scenario: Deleting an image
- **WHEN** the admin activates a row's image `Delete` action
- **THEN** the page calls `DELETE /api/exercises/:id/image` and on success the row shows
  the placeholder, leaving the exercise and its name unchanged

#### Scenario: Image delete fails
- **WHEN** the image delete call returns an error
- **THEN** the row keeps its thumbnail and shows the error

#### Scenario: Image actions do not affect name editing
- **WHEN** the admin has an unsaved name edit and uploads an image on the same row
- **THEN** the name edit and its Save/Revert buttons are unchanged

## Decisions

- Serve images through the AdminApi Worker (`GET /api/exercises/:id/image`) instead of a
  public R2 bucket or custom domain. This keeps images behind Cloudflare Access and
  needs no extra hostname. A public bucket would be right if images ever needed to load
  without authentication or with a CDN in front for scale, neither of which a
  single-user app needs. The serve route is readable by the `User` role so the workout
  app can show the image. The workout app is a different origin with its own Worker, so
  it cannot call this Worker's route from the browser; the future `WorkoutApi` Worker
  will bind the same bucket and expose the same `GET /api/exercises/:id/image` shape
  (same role rule) rather than reaching across origins. A shared-origin or CORS setup
  would be right only if the two apps ever merged into one Worker.
- Send raw bytes in `PUT` instead of `multipart/form-data`. There is one file and no other
  fields, so a raw body avoids a parser in the Worker and makes the size check a
  length check. Multipart would be right if the upload ever carried metadata.
- Store an R2 key on the exercise, not a full URL. The API derives `imageUrl` from it, so
  the bucket layout can change without a data migration. The roadmap says "URL persisted
  on the Exercise record"; this stores the key that URL is derived from.
- Enforce the 1 MB cap and the signature check in the Worker and again on the client.
  The Worker is authoritative; the client copy is for immediate feedback.
- Downscale on the client with a canvas via JS interop. Blazor WASM ships no image
  library, and resizing before upload keeps R2 and bandwidth use small. Server-side
  resizing would be right if the Worker later had to accept untrusted third-party
  uploads.
- Keep the source format (no PNG to JPEG re-encode). The source images are illustrations
  where PNG is lossless and JPEG adds edge artifacts. Re-encoding would be right if large
  PNG photos became common.
- Model the row's image as distinct `NoImage`, `HasImage`, and `StagedImage` types, and
  upload/delete results as success/failure outcome types, per
  `CLAUDE-domain-driven-design.md`.
- Display with contain-fit inside a fixed box rather than forcing an aspect ratio. Source
  heights range from about 150 px to 1400 px at 700 px wide. A cover-fit with enforced
  ratio would be right if the admin wanted uniform tiles and accepted cropping or
  rejection at upload.
- Image changes are allowed on exercises in use by programs; only deleting the exercise
  itself is guarded. The image is a property of the library entry, so a program picks up
  the new picture immediately, which is the point of a shared library. A guard would be
  right if a program ever needed a frozen picture, which would call for per-program
  images instead.
- The Worker sniffs and validates only JPEG and PNG signatures. WebP and others are a
  later addition, not a rejected design.

