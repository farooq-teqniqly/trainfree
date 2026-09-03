-- Migration number: 0007 	 2026-09-02T00:00:00.000Z
CREATE UNIQUE INDEX idx_phases_name_nocase ON phases (name COLLATE NOCASE);
