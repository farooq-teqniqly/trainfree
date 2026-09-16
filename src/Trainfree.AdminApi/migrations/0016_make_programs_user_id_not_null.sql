-- Migration number: 0016 	 2026-09-16T00:00:00.000Z
-- Closes the "ownerless program" gap flagged in PR #129 review: a nullable user_id let
-- createProgram silently record NULL when identity resolution produced no usable
-- userId. SQLite/D1 has no ALTER COLUMN, so the column is tightened by rebuilding the
-- table -- confirmed via a live query against trainfree_db that the only existing row
-- already has a non-null user_id, so this rebuild is lossless.
CREATE TABLE programs_new (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    program_id TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    user_id TEXT NOT NULL REFERENCES users(user_id),
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

INSERT INTO programs_new (id, program_id, name, user_id, created_at, updated_at)
SELECT id, program_id, name, user_id, created_at, updated_at FROM programs;

DROP TABLE programs;
ALTER TABLE programs_new RENAME TO programs;

CREATE UNIQUE INDEX idx_programs_name_nocase ON programs (name COLLATE NOCASE);
