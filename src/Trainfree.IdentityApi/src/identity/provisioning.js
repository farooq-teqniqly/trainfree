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

function isUniqueConstraintViolation(err) {
    return typeof err?.message === "string" && err.message.includes("UNIQUE constraint failed");
}

// Re-reads the fully provisioned pair after a unique-constraint violation, for when a
// concurrent call won the race this one lost. Returns `{ created: false }` if the pair
// is now complete, otherwise rethrows the original error -- the violation was for some
// other reason (e.g. two different concurrent identities happened to collide on
// generated `user_id`s, which this can't repair).
async function convergeOnConcurrentWinner(db, err, { providerName, email }) {
    if (!isUniqueConstraintViolation(err)) {
        throw err;
    }

    const winner = await db
        .prepare(
            `SELECT users.user_id as userId
             FROM logins
             JOIN users ON users.login_id = logins.id
             WHERE logins.provider_name = ? AND logins.provider_id = ?`,
        )
        .bind(providerName, email)
        .first();
    if (!winner) {
        throw err;
    }

    return { created: false };
}

// Adds a user's D1 identity (a logins row and its paired users row) after their email
// is whitelisted in Cloudflare Access. Idempotent on the fully provisioned pair -- a
// logins row alone (with no paired users row) would never resolve a role and would be
// permanently unauthenticatable, so a fully-provisioned result requires both. Three
// cases:
//   1. Both rows already exist -- no-op, `{ created: false }`.
//   2. A `logins` row exists with no paired `users` row (an orphan -- e.g. a previous
//      provisioning attempt raced and lost, see case 3) -- attach the missing `users`
//      row to the existing login rather than trying to re-insert `logins`, which would
//      violate its own uniqueness constraint and make the orphan permanently
//      unrepairable. Two concurrent repairs of the same orphan race on `users.login_id`
//      (also unique), so this insert gets the same converge-on-the-winner recovery as
//      case 3.
//   3. Neither row exists -- insert both in a single db.batch() so a failure partway
//      through leaves neither row present. If a concurrent call wins the race on the
//      `logins` unique constraint, re-read the pair the winner completed and converge
//      on `{ created: false }` instead of surfacing the raw constraint error.
export async function provisionIdentity(db, { email, providerName, roleName }) {
    const existingLogin = await db
        .prepare(
            `SELECT logins.id as loginId, users.user_id as userId
             FROM logins
             LEFT JOIN users ON users.login_id = logins.id
             WHERE logins.provider_name = ? AND logins.provider_id = ?`,
        )
        .bind(providerName, email)
        .first();

    if (existingLogin?.userId) {
        return { created: false };
    }

    // Resolved only once the identity is known to need it (the orphan/new-insert
    // branches below) -- looking this up unconditionally would throw UnknownRoleError
    // for an already-provisioned identity whenever `--role` names a role that was since
    // renamed/removed, when the correct result for that identity is the no-op above.
    const role = await db.prepare("SELECT role_id as roleId FROM roles WHERE name = ?").bind(roleName).first();
    if (!role) {
        throw new UnknownRoleError(roleName);
    }

    const now = new Date().toISOString();

    if (existingLogin) {
        const userId = generateUserId();
        try {
            await db
                .prepare(
                    "INSERT INTO users (user_id, login_id, role_id, created_at, updated_at) VALUES (?, ?, ?, ?, ?)",
                )
                .bind(userId, existingLogin.loginId, role.roleId, now, now)
                .run();
        } catch (err) {
            return await convergeOnConcurrentWinner(db, err, { providerName, email });
        }
        return { created: true, userId };
    }

    const userId = generateUserId();

    try {
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
    } catch (err) {
        return await convergeOnConcurrentWinner(db, err, { providerName, email });
    }

    return { created: true, userId };
}
