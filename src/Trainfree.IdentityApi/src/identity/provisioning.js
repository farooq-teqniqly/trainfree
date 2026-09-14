const USER_ID_BODY_LENGTH = 6;
const USER_ID_ALPHABET = "ABCDEFGHJKMNPQRSTVWXYZ23456789";
const USER_ID_PREFIX = "USR-";

function generateUserId() {
    const randomBytes = new Uint8Array(USER_ID_BODY_LENGTH);
    crypto.getRandomValues(randomBytes);

    let body = "";
    for (const byte of randomBytes) {
        body += USER_ID_ALPHABET[byte % USER_ID_ALPHABET.length];
    }
    return USER_ID_PREFIX + body;
}

// Thrown when the caller names a role that doesn't exist in `roles` -- the script never
// creates a role on the fly, per spec's "looking up role_id by name (never creating a
// role)."
export class UnknownRoleError extends Error {
    constructor(roleName) {
        super(`No role named "${roleName}" exists`);
        this.name = "UnknownRoleError";
    }
}

// Adds a user's D1 identity (a logins row and its paired users row) after their email
// is whitelisted in Cloudflare Access. Idempotent on the fully provisioned pair -- a
// logins row alone (with no paired users row) would never resolve a role and would be
// permanently unauthenticatable, so the check covers both, never just `logins`. When no
// identity exists yet, both rows are inserted in a single db.batch() call so a failure
// partway through leaves neither row present rather than an orphaned `logins` row.
export async function provisionIdentity(db, { email, providerName, roleName }) {
    const role = await db.prepare("SELECT role_id as roleId FROM roles WHERE name = ?").bind(roleName).first();
    if (!role) {
        throw new UnknownRoleError(roleName);
    }

    const existing = await db
        .prepare(
            `SELECT users.user_id as userId
             FROM logins
             JOIN users ON users.login_id = logins.id
             WHERE logins.provider_name = ? AND logins.provider_id = ?`,
        )
        .bind(providerName, email)
        .first();
    if (existing) {
        return { created: false };
    }

    const userId = generateUserId();
    const now = new Date().toISOString();

    await db.batch([
        db
            .prepare(
                "INSERT INTO logins (provider_name, provider_id, created_at, updated_at) VALUES (?, ?, ?, ?)",
            )
            .bind(providerName, email, now, now),
        db
            .prepare(
                `INSERT INTO users (user_id, login_id, role_id, created_at, updated_at)
                 SELECT ?, id, ?, ?, ? FROM logins WHERE provider_name = ? AND provider_id = ?`,
            )
            .bind(userId, role.roleId, now, now, providerName, email),
    ]);

    return { created: true, userId };
}
