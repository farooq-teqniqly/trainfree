import { applyD1Migrations, env } from "cloudflare:test";

await applyD1Migrations(env.DB, env.TEST_MIGRATIONS);

// Stage-one only: programs.user_id still has a foreign key to users(user_id), so the fixed
// synthetic identity identity.js returns under LOCAL_DEV_BYPASS (USR-LOCALDEV) needs a
// matching users row for any test that creates a program. identity.js itself does no
// lookup. Remove this seed, with scripts/seed-local-dev-user.sql, when the stage-two
// baseline migration drops the foreign key.
const administratorRole = await env.DB.prepare(
    "SELECT role_id as roleId FROM roles WHERE name = 'Administrator'",
).first();
const now = new Date().toISOString();
await env.DB.batch([
    env.DB.prepare(
        "INSERT INTO logins (provider_name, provider_id, created_at, updated_at) VALUES (?, ?, ?, ?)",
    ).bind("cloudflare-access", "local-dev@trainfree.local", now, now),
    env.DB.prepare(
        `INSERT INTO users (user_id, login_id, role_id, created_at, updated_at)
         SELECT ?, id, ?, ?, ? FROM logins WHERE provider_name = ? AND provider_id = ?`,
    ).bind(
        "USR-LOCALDEV",
        administratorRole.roleId,
        now,
        now,
        "cloudflare-access",
        "local-dev@trainfree.local",
    ),
]);
