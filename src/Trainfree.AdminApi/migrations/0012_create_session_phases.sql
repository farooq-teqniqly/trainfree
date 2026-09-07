-- Migration number: 0012 	 2026-09-06T00:00:00.000Z
CREATE TABLE session_phases (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_phase_id TEXT NOT NULL UNIQUE,
    session_id TEXT NOT NULL REFERENCES sessions(session_id) ON DELETE CASCADE,
    phase_id TEXT NOT NULL REFERENCES phases(phase_id),
    created_at TEXT NOT NULL
);

-- Matches listSessionPhases' lookup by session_id and the phase delete guard's lookup
-- by phase_id (session-phases.js, phases.js).
CREATE INDEX idx_session_phases_session_id ON session_phases (session_id);
CREATE INDEX idx_session_phases_phase_id ON session_phases (phase_id);
