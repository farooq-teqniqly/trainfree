# Intent: exercise-image-upload

Frozen anchor for the `trainfree-lean` review gate (issue #68). Quoted from
`docs/trainfree-roadmap.md` slice 14 as it stood on `main` before this change edited it.

## Roadmap slice 14 (verbatim)

14. **`add-exercise-images-r2`** -- Wires up the upload control already shown in slice 6's
    `Exercises` page, R2 bucket storage, URL persisted on the `Exercise` record, image
    displayed both there and during the `Trainfree.Workout` runner (screens 3-4). Depends
    on slice 6 (`Exercise` entity must exist) and benefits from slice 10/11 being in place
    to see it rendered live, but is not blocked by 9-13 -- can slot in parallel after
    slice 6 if desired.

## Narrowing request

The request that opened this change narrowed slice 14 and added the image-delete,
exercise-delete cleanup, bulk-import, and cropping rows of the spec's `## Requirement
coverage` table (rows 6-9). Its verbatim text was not preserved in the repo when the
change was proposed, so those rows have no frozen source beyond this note.
