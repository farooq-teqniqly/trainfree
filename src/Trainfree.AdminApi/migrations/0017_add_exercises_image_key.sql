-- Migration number: 0017 	 2026-09-30T00:00:00.000Z
-- Nullable, no default: existing exercises have no image. Holds the R2 object key of
-- the exercise's image; never exposed to clients (they get a Worker-served imageUrl).
ALTER TABLE exercises ADD COLUMN image_key TEXT;
