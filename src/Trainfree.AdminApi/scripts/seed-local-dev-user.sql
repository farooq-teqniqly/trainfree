-- Local-only. Run with `npm run db:seed:local`; never against --remote.
-- Stage-one workaround: programs.user_id still has a foreign key to users(user_id), so the
-- fixed synthetic identity in src/identity.js (USR-LOCALDEV) needs a matching row. Delete
-- this file, the npm script and test/apply-migrations.js's seed when the stage-two baseline
-- migration removes that foreign key.
INSERT OR IGNORE INTO logins (provider_name, provider_id, created_at, updated_at)
VALUES ('cloudflare-access', 'local-dev@trainfree.local', datetime('now'), datetime('now'));

INSERT OR IGNORE INTO users (user_id, login_id, role_id, created_at, updated_at)
SELECT 'USR-LOCALDEV', logins.id, 'ROL-A3F7K2', datetime('now'), datetime('now')
FROM logins
WHERE provider_name = 'cloudflare-access' AND provider_id = 'local-dev@trainfree.local';
