# Frozen anchor: `docs/trainfree-roadmap.md` slice 7

Quoted verbatim from `docs/trainfree-roadmap.md` at commit `c006baf` (the commit that
last touched the roadmap file, and the commit that was already `HEAD` before any
implementation work on this change began -- so this text is unchanged from the state
the change was implemented against).

---

7. **`add-session-phases-crud`** -- Extends admin CRUD with a per-session `SessionPhase`
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
