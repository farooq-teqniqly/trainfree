// The IdentityApi service-binding contract this module speaks, mirroring
// Trainfree.IdentityApi/src/shared/providers.js's PROVIDER_NAME_CLOUDFLARE_ACCESS and
// smoke-harness/check.js's request shape -- kept as literal constants here rather than
// imported, since AdminApi and IdentityApi are independent npm packages with no shared
// workspace linking.
const IDENTITY_CALLER = "admin";
const LOCAL_DEV_PROVIDER_NAME = "cloudflare-access";
const LOCAL_DEV_PROVIDER_ID = "local-dev@trainfree.local";

// URL host is arbitrary -- service bindings route on the binding, not the URL -- but the
// path must be the real route IdentityApi's Worker handles.
const IDENTITY_URL = "https://trainfree-identity-api/internal/identity";

// Substitutes for the real IdentityApi call when LOCAL_DEV_BYPASS is set. Throws rather
// than returning a bad userId when the seed row (from IdentityApi's
// provision-identity.js script -- see README.md's "Local development" section) is
// missing, so a forgotten seed step fails loudly instead of silently.
async function resolveLocalDevIdentity(db) {
    const row = await db
        .prepare(
            `SELECT users.user_id as userId
             FROM logins
             JOIN users ON users.login_id = logins.id
             WHERE logins.provider_name = ? AND logins.provider_id = ?`,
        )
        .bind(LOCAL_DEV_PROVIDER_NAME, LOCAL_DEV_PROVIDER_ID)
        .first();

    if (!row?.userId) {
        throw new Error(
            `LOCAL_DEV_BYPASS is set but no local-dev identity is seeded (looked for ` +
                `provider_id "${LOCAL_DEV_PROVIDER_ID}"). Run IdentityApi's provision ` +
                `script -- see README.md's "Local development" section.`,
        );
    }

    return { email: LOCAL_DEV_PROVIDER_ID, role: "Administrator", userId: row.userId };
}

// Calls IdentityApi's GET /internal/identity over the IDENTITY service binding and
// normalizes the result: { ok: true, identity } for a 200, or { ok: false, status } for
// everything else (IdentityApi's own 401/403 relayed verbatim; its 503, any other
// unexpected status including a missing/wrong-key 404, or a service-binding call that
// itself throws or times out, all normalized to 503 -- see spec's "IdentityApi outages
// surface as 503, never folded into 403").
async function callIdentityApi(request, env) {
    if (!env.IDENTITY || !env.ADMIN_INTERNAL_KEY) {
        return { ok: false, status: 503 };
    }

    let response;
    try {
        response = await env.IDENTITY.fetch(IDENTITY_URL, {
            headers: {
                "X-Trainfree-Caller": IDENTITY_CALLER,
                "X-Trainfree-Internal-Key": env.ADMIN_INTERNAL_KEY,
                Cookie: request.headers.get("Cookie") ?? "",
            },
        });
    } catch (err) {
        console.error("IdentityApi service-binding call failed", err);
        return { ok: false, status: 503 };
    }

    if (response.status === 401 || response.status === 403) {
        return { ok: false, status: response.status };
    }

    if (response.status !== 200) {
        return { ok: false, status: 503 };
    }

    let identity;
    try {
        identity = await response.json();
    } catch (err) {
        console.error("IdentityApi returned a 200 with an unparseable body", err);
        return { ok: false, status: 503 };
    }
    return { ok: true, identity };
}

// Resolves the caller's identity: the synthetic local-dev identity when
// env.LOCAL_DEV_BYPASS is set (never in a deployed environment -- see
// wrangler.deploy.jsonc), otherwise the real IdentityApi service-binding call.
export async function checkIdentity(request, env) {
    if (env.LOCAL_DEV_BYPASS) {
        const identity = await resolveLocalDevIdentity(env.DB);
        return { ok: true, identity };
    }

    return callIdentityApi(request, env);
}

export function isAdministrator(identity) {
    return identity?.role === "Administrator";
}
