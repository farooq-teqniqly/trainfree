-- Migration number: 0008 	 2026-09-02T00:00:00.000Z
-- `categories` is kept (not dropped) so the still-deploying old Worker keeps working
-- against it during the deploy window; a follow-up migration drops it once the new
-- Worker is confirmed live.
INSERT INTO phases (phase_id, name, created_at, updated_at)
SELECT REPLACE(category_id, 'CAT-', 'PHS-'), name, created_at, updated_at
FROM categories;
