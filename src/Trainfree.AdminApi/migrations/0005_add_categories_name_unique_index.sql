-- Migration number: 0005 	 2026-08-30T00:00:00.000Z
CREATE UNIQUE INDEX idx_categories_name_nocase ON categories (name COLLATE NOCASE);
