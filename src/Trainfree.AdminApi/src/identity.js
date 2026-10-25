import { setSpanAttributes } from "./span-attributes.js";

// The cloudfare-identity service-binding contract this module speaks, kept as literal
// constants since cloudfare-identity is a separate repo with no shared package.
const IDENTITY_APPLICATION = "Trainfree.Admin";

// URL host is arbitrary -- service bindings route on the binding, not the URL -- but the
// path must be the real route cloudfare-identity handles.
const IDENTITY_URL = "https://cloudfare-identity/v1/identity";

// Access sets this on requests to a gated Worker; unlike the CF_Authorization cookie it
// is not rewritten. Pending the production check in tasks.md 1.4.
const ACCESS_TOKEN_HEADER = "Cf-Access-Jwt-Assertion";

// Fixed synthetic identity used when LOCAL_DEV_BYPASS is set. No database lookup.
const LOCAL_DEV_IDENTITY = Object.freeze({
    email: "local-dev@trainfree.local",
    role: "Administrator",
    userId: "USR-LOCALDEV",
});

// Calls cloudfare-identity's GET /v1/identity over the IDENTITY service binding and
// normalizes the result: { ok: true, identity } for a 200, or { ok: false, status } for
// everything else (its own 401/403 relayed verbatim; its 503, any other unexpected
// status including a 404 for an unregistered application or wrong credential, or a
// service-binding call that itself throws or times out, all normalized to 503 -- see
// spec's "Identity service outages surface as 503, never folded into 403"). The token is
// forwarded, never verified here.
async function callIdentityService(request, env) {
    if (!env.IDENTITY || !env.ADMIN_INTERNAL_KEY) {
        return { ok: false, status: 503 };
    }

    const headers = {
        "X-Identity-Application": IDENTITY_APPLICATION,
        "X-Identity-Credential": env.ADMIN_INTERNAL_KEY,
    };
    const token = request.headers.get(ACCESS_TOKEN_HEADER);
    if (token) {
        headers["X-Identity-Token"] = token;
    }

    let response;
    try {
        response = await env.IDENTITY.fetch(IDENTITY_URL, { headers });
    } catch (err) {
        console.error("cloudfare-identity service-binding call failed", err);
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
        console.error("cloudfare-identity returned a 200 with an unparseable body", err);
        return { ok: false, status: 503 };
    }

    if (!isValidIdentity(identity)) {
        console.error("cloudfare-identity returned a 200 with a malformed identity body", identity);
        return { ok: false, status: 503 };
    }

    return { ok: true, identity: withoutInvalidSessionId(identity) };
}

const VALID_ROLES = new Set(["Administrator", "User"]);

// cloudfare-identity's one-way, per-login session id: 32 lowercase hex characters.
const SESSION_ID_PATTERN = /^[0-9a-f]{32}$/;

// Guards against an incomplete-but-parseable 200 response (e.g. a valid Administrator
// role with no usable userId) reaching a caller like createProgram, which would
// otherwise silently record an ownerless row -- see PR #129 review discussion. Also
// rejects a role cloudfare-identity doesn't define (e.g. a future/typo'd value), rather than
// relaying it verbatim to a client via GET /api/me.
function isValidIdentity(identity) {
    return (
        typeof identity?.email === "string" &&
        identity.email.length > 0 &&
        typeof identity?.userId === "string" &&
        identity.userId.length > 0 &&
        VALID_ROLES.has(identity?.role)
    );
}

// sessionId is optional observability metadata, so a malformed one is dropped (the
// identity still resolves, just untagged with session.id) instead of failing the request
// the way a malformed email/userId/role does.
function withoutInvalidSessionId(identity) {
    const { sessionId, ...rest } = identity;
    if (sessionId === undefined) {
        return rest;
    }
    if (typeof sessionId === "string" && SESSION_ID_PATTERN.test(sessionId)) {
        return identity;
    }
    console.warn("cloudfare-identity returned a malformed sessionId; ignoring it");
    return rest;
}

// Resolves the caller's identity: the synthetic local-dev identity when
// env.LOCAL_DEV_BYPASS is set (never in a deployed environment -- see
// wrangler.deploy.jsonc), otherwise the real cloudfare-identity service-binding call. Tags the
// active span with the resolved identity (observability only); a failure leaves it
// untagged. getSpan is a test seam.
export async function checkIdentity(request, env, getSpan) {
    const result = await resolveIdentity(request, env);
    if (result.ok) {
        const { userId, role, sessionId } = result.identity;
        setSpanAttributes(
            { "user.id": userId, "user.role": role, "session.id": sessionId },
            getSpan,
        );
    }
    return result;
}

async function resolveIdentity(request, env) {
    if (env.LOCAL_DEV_BYPASS === "true") {
        return { ok: true, identity: { ...LOCAL_DEV_IDENTITY } };
    }

    return callIdentityService(request, env);
}

export function isAdministrator(identity) {
    return identity?.role === "Administrator";
}
