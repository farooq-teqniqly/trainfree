-- Migration number: 0004 	 2026-08-30T00:00:00.000Z
CREATE TABLE categories (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    category_id TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);
