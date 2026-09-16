-- Migration number: 0016 	 2026-09-16T00:00:00.000Z
-- Closes the "ownerless program" gap flagged in PR #129 review: a nullable user_id let
-- createProgram silently record NULL when identity resolution produced no usable
-- userId.
--
-- Rollout-ordering note (PR #129 review, Copilot): deploy.yaml applies migrations
-- before deploying the new Worker code, so for the short window between this
-- migration landing and the new Worker going live, POST /api/programs would still be
-- served by the previously-deployed Worker, whose createProgram omits user_id
-- entirely -- that INSERT would now abort with 500 instead of the previous 201.
-- Accepted for this single-user, self-hosted app (see CLAUDE.md): the window is one
-- CI job's worth of seconds, and the only caller who could hit it is the app's owner
-- creating a program at that exact moment. A schema change that must never reject an
-- old Worker's writes needs an expand/contract migration split across two deploys;
-- not worth that process overhead here, but a future migration with a wider
-- blast radius (multi-user, or a longer deploy window) should reconsider this.
--
-- Enforced via triggers, not a NOT NULL column rebuild: D1/SQLite has no ALTER COLUMN,
-- so tightening a column requires the documented rebuild-and-rename dance (CREATE
-- replacement table, copy rows, DROP the original, rename). That DROP is the trap --
-- confirmed empirically against a local D1 instance that dropping `programs` while
-- `sessions.program_id REFERENCES programs(program_id) ON DELETE CASCADE` is live
-- performs SQLite's documented implicit "DELETE FROM programs" before the drop, which
-- cascades through sessions -> session_phases -> program_exercises and destroys all of
-- it -- and neither `PRAGMA foreign_keys=OFF` nor renaming `programs` out of the way
-- first prevents this (D1 applies a migration file as one transaction, where toggling
-- the pragma is a documented no-op, and SQLite's ALTER TABLE RENAME auto-updates child
-- FK clauses to the new name, so the drop still cascades regardless of order). A
-- five-table rebuild (programs plus every table transitively FK'd to it) would work but
-- multiplies the surface for exactly this kind of mistake for no real benefit over a
-- trigger, which enforces the same invariant without ever touching existing rows.
CREATE TRIGGER trg_programs_user_id_not_null_insert
BEFORE INSERT ON programs
WHEN NEW.user_id IS NULL
BEGIN
    SELECT RAISE(ABORT, 'programs.user_id must not be NULL');
END;

-- Scoped to "OF user_id" (fires only when an UPDATE's SET list names that column), not
-- every UPDATE: a legacy pre-0015 row with no owner could otherwise never be renamed --
-- SQLite carries a column's old value into NEW for any UPDATE that doesn't set it, so an
-- unscoped trigger would see NEW.user_id IS NULL and reject a rename that never touched
-- user_id at all.
CREATE TRIGGER trg_programs_user_id_not_null_update
BEFORE UPDATE OF user_id ON programs
WHEN NEW.user_id IS NULL
BEGIN
    SELECT RAISE(ABORT, 'programs.user_id must not be NULL');
END;
