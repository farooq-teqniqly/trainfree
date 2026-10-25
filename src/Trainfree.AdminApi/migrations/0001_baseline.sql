-- Migration number: 0001 	 2026-10-24T00:00:00.000Z
-- Baseline for the stage-two database (issue #158). It replaces the former migrations
-- 0001-0017, whose history lives in git, and describes the app tables exactly as those
-- migrations left them, minus the identity tables (logins, users, roles; identity now
-- lives in cloudfare-identity) and the foreign key from programs.user_id to users.
-- The original trainfree_db is no longer migrated.
CREATE TABLE programs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    program_id TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    user_id TEXT
);

CREATE UNIQUE INDEX idx_programs_name_nocase ON programs (name COLLATE NOCASE);

-- Triggers, not a NOT NULL column: SQLite cannot alter a column in place, and rebuilding
-- `programs` would cascade-delete the whole tree through sessions. The UPDATE trigger is
-- scoped to "OF user_id" so a rename that does not name user_id is not rejected.
CREATE TRIGGER trg_programs_user_id_not_null_insert
BEFORE INSERT ON programs
WHEN NEW.user_id IS NULL
BEGIN
    SELECT RAISE(ABORT, 'programs.user_id must not be NULL');
END;

CREATE TRIGGER trg_programs_user_id_not_null_update
BEFORE UPDATE OF user_id ON programs
WHEN NEW.user_id IS NULL
BEGIN
    SELECT RAISE(ABORT, 'programs.user_id must not be NULL');
END;

CREATE TABLE sessions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id TEXT NOT NULL UNIQUE,
    program_id TEXT NOT NULL REFERENCES programs(program_id) ON DELETE CASCADE,
    name TEXT NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE UNIQUE INDEX idx_sessions_program_name_nocase ON sessions (program_id, name COLLATE NOCASE);

CREATE TABLE phases (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    phase_id TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE UNIQUE INDEX idx_phases_name_nocase ON phases (name COLLATE NOCASE);

CREATE TABLE exercises (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    exercise_id TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    image_key TEXT
);

CREATE UNIQUE INDEX idx_exercises_name_nocase ON exercises (name COLLATE NOCASE);

CREATE TABLE session_phases (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_phase_id TEXT NOT NULL UNIQUE,
    session_id TEXT NOT NULL REFERENCES sessions(session_id) ON DELETE CASCADE,
    phase_id TEXT NOT NULL REFERENCES phases(phase_id),
    created_at TEXT NOT NULL
);

CREATE INDEX idx_session_phases_session_id ON session_phases (session_id);
CREATE INDEX idx_session_phases_phase_id ON session_phases (phase_id);

CREATE TABLE program_exercises (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    program_exercise_id TEXT NOT NULL UNIQUE,
    session_phase_id TEXT NOT NULL REFERENCES session_phases(session_phase_id) ON DELETE CASCADE,
    exercise_id TEXT NOT NULL REFERENCES exercises(exercise_id),
    type TEXT NOT NULL CHECK (type IN ('Reps', 'Timed')),
    reps INTEGER,
    duration_seconds INTEGER,
    weight REAL NOT NULL,
    sets INTEGER NOT NULL,
    rest_seconds INTEGER NOT NULL,
    side TEXT NOT NULL CHECK (side IN ('Both', 'Left', 'Right')),
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE INDEX idx_program_exercises_session_phase_id ON program_exercises (session_phase_id);
CREATE INDEX idx_program_exercises_exercise_id ON program_exercises (exercise_id);
