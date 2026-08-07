-- Migration number: 0002 	 2026-08-06T00:00:00.000Z
CREATE UNIQUE INDEX idx_programs_name_nocase ON programs (name COLLATE NOCASE);
