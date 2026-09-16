import { applyD1Migrations, env } from "cloudflare:test";

await applyD1Migrations(env.DB, env.TEST_MIGRATIONS);

// wrangler.jsonc sets LOCAL_DEV_BYPASS by default (see that file's comment), and
// vitest.config.js points vitest-pool-workers at this same config -- so every
// SELF.fetch call in index.test.js resolves its identity via identity.js's
// LOCAL_DEV_BYPASS branch, which requires this exact seed row to exist. Without it,
// every enforced route would fail closed with "no local-dev identity is seeded" instead
// of exercising the route under test. Mirrors the shape IdentityApi's
// provision-identity.js script would create in real local dev, but inlined here since
// AdminApi's test pool has no IdentityApi Worker to call.
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
