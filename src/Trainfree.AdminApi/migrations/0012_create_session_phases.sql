-- Migration number: 0012 	 2026-09-06T00:00:00.000Z
CREATE TABLE session_phases (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_phase_id TEXT NOT NULL UNIQUE,
    session_id TEXT NOT NULL REFERENCES sessions(session_id) ON DELETE CASCADE,
    phase_id TEXT NOT NULL REFERENCES phases(phase_id),
    created_at TEXT NOT NULL
);
