-- Migration number: 0011 	 2026-09-05T00:00:00.000Z
CREATE UNIQUE INDEX idx_exercises_name_nocase ON exercises (name COLLATE NOCASE);
