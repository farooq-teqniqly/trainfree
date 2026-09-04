-- Migration number: 0009 	 2026-09-04T00:00:00.000Z
-- Finalizes the cleanup migration 0008 deferred: the Worker that replaced `categories`
-- with `phases` (#60) is confirmed live in production as of v0.3.0 (commit 9142fa7), so
-- the deprecated table is safe to drop.
DROP TABLE categories;
