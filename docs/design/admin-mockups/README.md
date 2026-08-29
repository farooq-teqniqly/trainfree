# Admin app mockups

Source files for the Trainfree Admin design canvas -- hi-fi mockups of
`src/Trainfree.Admin`, following `CLAUDE-blazor-ui.md` and `docs/ui-decisions.md`
(Inter, dark navbar / light `bg-body-tertiary` sidebar, black `.btn-primary`).
Static mockups, not a clickable prototype.

Live canvas: https://claude.ai/code/artifact/a75bff9d-19c2-4707-95c9-999c3687964b

## Screens

- `Home.dc.html` -- landing page, quick links into Programs, Categories and
  Exercises.
- `EmptyState.dc.html` -- Programs, before any program exists.
- `Main.dc.html` -- Programs, the full nested spreadsheet (Program > Session >
  Category > Exercise), sized at 1920px to prove out a wide desktop monitor.
  Category rows pick from the canonical category library and exercise rows from
  the canonical exercise library, both instead of free text -- see the open
  pickers on the "Warm Up" and "Bodyweight Squat" rows.
- `ExercisesEmpty.dc.html` -- Exercises, before any exercise exists.
- `Exercises.dc.html` -- Exercises, the canonical name/image/type library that
  `Main.dc.html`'s exercise picker draws from.
- `CategoriesEmpty.dc.html` -- Categories, before any category exists.
- `Categories.dc.html` -- Categories, the canonical name library (e.g. "Warm Up",
  "A") that `Main.dc.html`'s category picker draws from.
- `canvas.json` -- layout manifest: artboard positions and the flow annotations
  connecting the empty states to their populated screens.

Icons are hand-drawn inline SVG (matching the Bootstrap Icons style), not the
`bi-*` icon font -- the canvas's sandbox can't load Bootstrap Icons' CSS from a
CDN. Implementation should keep using `bi-*` per `CLAUDE-blazor-ui.md`.

## Continuing to refine

These are the working files for Claude Design's canvas editor (a Claude Code
preview feature). To pick the design back up in a Claude Code session:

1. Point Claude at this folder and the live canvas URL above.
2. Ask it to update a screen, or re-seed from scratch with `/design`'s
   `seed-canvas.mjs` helper (the `design` skill's "Updating an existing canvas"
   section covers both the from-working-files and read-back-from-the-artifact
   paths).

Anyone with edit access to the artifact can also tweak it directly in the
canvas's WYSIWYG editor and hit Save -- that becomes the new live version but
does not update these files; re-read the artifact to bring changes back here.
