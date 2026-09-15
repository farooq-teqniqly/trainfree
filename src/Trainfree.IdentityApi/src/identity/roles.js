// Resolves a provisioned identity's userId and role from D1, matching a `logins` row
// on both `provider_name` and `provider_id` together (never `provider_id`/email
// alone -- the schema's `UNIQUE (provider_name, provider_id)` constraint permits the
// same provider_id to recur under a different provider_name once a second provider
// exists). Runs a fresh query on every call: no caching/memoization, so a role change
// in D1 is effective on the very next call.
export async function lookupIdentity(db, { providerName, providerId }) {
    const row = await db
        .prepare(
            `SELECT users.user_id as userId, roles.name as role
             FROM logins
             JOIN users ON users.login_id = logins.id
             LEFT JOIN roles ON roles.role_id = users.role_id
             WHERE logins.provider_name = ? AND logins.provider_id = ?`,
        )
        .bind(providerName, providerId)
        .first();

    return row ?? null;
}
