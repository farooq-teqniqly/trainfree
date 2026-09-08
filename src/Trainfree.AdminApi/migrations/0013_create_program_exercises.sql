-- Migration number: 0013 	 2026-09-06T00:00:00.000Z
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

-- Matches listProgramExercises' lookup by session_phase_id and the exercise delete
-- guard's lookup by exercise_id (program-exercises.js, exercises.js).
CREATE INDEX idx_program_exercises_session_phase_id ON program_exercises (session_phase_id);
CREATE INDEX idx_program_exercises_exercise_id ON program_exercises (exercise_id);
