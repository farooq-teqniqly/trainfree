-- Migration number: 0014 	 2026-09-13T00:00:00.000Z
-- Owned by AdminApi's migration history per CLAUDE.md and
-- specs/identity-api/spec.md: IdentityApi has no migration step of its own, so its
-- schema rides here even though IdentityApi is the Worker that reads/writes it.
CREATE TABLE roles (
    role_id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE
);

INSERT INTO roles (name) VALUES ('Administrator'), ('User');

CREATE TABLE logins (
    login_id INTEGER PRIMARY KEY AUTOINCREMENT,
    provider_name TEXT NOT NULL,
    provider_id TEXT NOT NULL,
    created_at TEXT NOT NULL,
    UNIQUE (provider_name, provider_id)
);

CREATE TABLE users (
    user_id INTEGER PRIMARY KEY AUTOINCREMENT,
    login_id INTEGER NOT NULL UNIQUE REFERENCES logins(login_id),
    role_id INTEGER REFERENCES roles(role_id),
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);
