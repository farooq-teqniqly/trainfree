-- Migration number: 0003 	 2026-08-29T00:00:00.000Z
CREATE TABLE sessions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id TEXT NOT NULL UNIQUE,
    program_id TEXT NOT NULL REFERENCES programs(program_id) ON DELETE CASCADE,
    name TEXT NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE UNIQUE INDEX idx_sessions_program_name_nocase ON sessions (program_id, name COLLATE NOCASE);
