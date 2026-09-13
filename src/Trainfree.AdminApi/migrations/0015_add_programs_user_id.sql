-- Migration number: 0015 	 2026-09-13T00:00:01.000Z
-- Nullable, no default: existing programs rows have no owner until slice 2 wires
-- AdminApi to populate it via IdentityApi's resolved userId.
ALTER TABLE programs ADD COLUMN user_id INTEGER REFERENCES users(user_id);
