-- Migration number: 0014 	 2026-09-13T00:00:00.000Z
-- Owned by AdminApi's migration history per CLAUDE.md and
-- specs/identity-api/spec.md: IdentityApi has no migration step of its own, so its
-- schema rides here even though IdentityApi is the Worker that reads/writes it.
--
-- role_id/user_id are Crockford Base32 business ids (prefix ROL-/USR-), matching the
-- surrogate-id-plus-business-id pattern already used by programs/sessions/phases/etc.
CREATE TABLE roles (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    role_id TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL UNIQUE,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

INSERT INTO roles (role_id, name, created_at, updated_at) VALUES
    ('ROL-A3F7K2', 'Administrator', '2026-09-13T00:00:00.000Z', '2026-09-13T00:00:00.000Z'),
    ('ROL-Q8Z4M6', 'User', '2026-09-13T00:00:00.000Z', '2026-09-13T00:00:00.000Z');

CREATE TABLE logins (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    provider_name TEXT NOT NULL,
    provider_id TEXT NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    UNIQUE (provider_name, provider_id)
);

CREATE TABLE users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id TEXT NOT NULL UNIQUE,
    login_id INTEGER NOT NULL UNIQUE REFERENCES logins(id),
    role_id TEXT REFERENCES roles(role_id),
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);
