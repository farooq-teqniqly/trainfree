import { createProgram, deleteProgram, listPrograms, renameProgram } from "./programs.js";
import {
    createSession,
    deleteSession,
    listSessions,
    programExists,
    renameSession,
} from "./sessions.js";
import {
    createPhase,
    deletePhase,
    listPhases,
    renamePhase,
} from "./phases.js";
import {
    validatePhaseName,
    validateProgramName,
    validateSessionName,
} from "./validation.js";
import { DuplicateNameError } from "./errors.js";
import { versionStamp } from "./version.js";

// The Worker and Blazor client are the same origin in production ([assets] + main share
// one deployment), so no request there ever carries a cross-origin Origin header and
// these headers are never needed. Locally, Blazor's dev server and `wrangler dev` run on
// different ports -- without CORS the browser silently blocks every fetch from the admin
// UI. Scoped to that one known dev origin rather than "*", so production never ships a
// blanket allow-any-origin policy for what CLAUDE.md documents as a single-origin app.
const DEV_ORIGIN = "http://localhost:5280";

function corsHeadersFor(request) {
    const origin = request.headers.get("Origin");
    if (origin !== DEV_ORIGIN) {
        return null;
    }

    return {
        "Access-Control-Allow-Origin": origin,
        "Access-Control-Allow-Methods": "GET, POST, PATCH, DELETE, OPTIONS",
        "Access-Control-Allow-Headers": "content-type",
    };
}

function withCors(response, request) {
    const corsHeaders = corsHeadersFor(request);
    if (!corsHeaders) {
        return response;
    }

    const headers = new Headers(response.headers);
    for (const [key, value] of Object.entries(corsHeaders)) {
        headers.set(key, value);
    }
    return new Response(response.body, { status: response.status, headers });
}

function jsonResponse(data, status = 200) {
    return new Response(JSON.stringify(data), {
        status,
        headers: { "content-type": "application/json" },
    });
}

function handleVersion(request, env) {
    if (request.method !== "GET") {
        return new Response("Method not allowed", { status: 405 });
    }

    // no-store, not just no-cache: this response is the one thing that must never be
    // answered from any cache, or the staleness check would itself go stale.
    const response = jsonResponse(versionStamp(env));
    response.headers.set("cache-control", "no-store");
    return response;
}

async function handleProgramsCollection(request, db) {
    if (request.method === "GET") {
        return jsonResponse(await listPrograms(db));
    }

    if (request.method === "POST") {
        const body = await request.json().catch(() => ({}));
        const validation = validateProgramName(body.name);
        if (!validation.valid) {
            return jsonResponse({ error: validation.error }, 400);
        }
        try {
            return jsonResponse(await createProgram(db, validation.name), 201);
        } catch (err) {
            if (err instanceof DuplicateNameError) {
                return jsonResponse({ error: err.message }, 409);
            }
            throw err;
        }
    }

    return new Response("Method not allowed", { status: 405 });
}

async function handleProgramResource(request, db, id) {
    if (request.method === "PATCH") {
        const body = await request.json().catch(() => ({}));
        const validation = validateProgramName(body.name);
        if (!validation.valid) {
            return jsonResponse({ error: validation.error }, 400);
        }
        try {
            const program = await renameProgram(db, id, validation.name);
            if (!program) {
                return jsonResponse({ error: "program not found" }, 404);
            }
            return jsonResponse(program);
        } catch (err) {
            if (err instanceof DuplicateNameError) {
                return jsonResponse({ error: err.message }, 409);
            }
            throw err;
        }
    }

    if (request.method === "DELETE") {
        const deleted = await deleteProgram(db, id);
        if (!deleted) {
            return jsonResponse({ error: "program not found" }, 404);
        }
        return new Response(null, { status: 204 });
    }

    return new Response("Method not allowed", { status: 405 });
}

async function handlePhasesCollection(request, db) {
    if (request.method === "GET") {
        return jsonResponse(await listPhases(db));
    }

    if (request.method === "POST") {
        const body = await request.json().catch(() => ({}));
        const validation = validatePhaseName(body.name);
        if (!validation.valid) {
            return jsonResponse({ error: validation.error }, 400);
        }
        try {
            return jsonResponse(await createPhase(db, validation.name), 201);
        } catch (err) {
            if (err instanceof DuplicateNameError) {
                return jsonResponse({ error: err.message }, 409);
            }
            throw err;
        }
    }

    return new Response("Method not allowed", { status: 405 });
}

async function handlePhaseResource(request, db, id) {
    if (request.method === "PATCH") {
        const body = await request.json().catch(() => ({}));
        const validation = validatePhaseName(body.name);
        if (!validation.valid) {
            return jsonResponse({ error: validation.error }, 400);
        }
        try {
            const phase = await renamePhase(db, id, validation.name);
            if (!phase) {
                return jsonResponse({ error: "phase not found" }, 404);
            }
            return jsonResponse(phase);
        } catch (err) {
            if (err instanceof DuplicateNameError) {
                return jsonResponse({ error: err.message }, 409);
            }
            throw err;
        }
    }

    if (request.method === "DELETE") {
        const deleted = await deletePhase(db, id);
        if (!deleted) {
            return jsonResponse({ error: "phase not found" }, 404);
        }
        return new Response(null, { status: 204 });
    }

    return new Response("Method not allowed", { status: 405 });
}

async function handleSessionsCollection(request, db, programId) {
    if (!(await programExists(db, programId))) {
        return jsonResponse({ error: "program not found" }, 404);
    }

    if (request.method === "GET") {
        return jsonResponse(await listSessions(db, programId));
    }

    if (request.method === "POST") {
        const body = await request.json().catch(() => ({}));
        const validation = validateSessionName(body.name);
        if (!validation.valid) {
            return jsonResponse({ error: validation.error }, 400);
        }
        try {
            return jsonResponse(await createSession(db, programId, validation.name), 201);
        } catch (err) {
            if (err instanceof DuplicateNameError) {
                return jsonResponse({ error: err.message }, 409);
            }
            throw err;
        }
    }

    return new Response("Method not allowed", { status: 405 });
}

async function handleSessionResource(request, db, programId, id) {
    if (request.method === "PATCH") {
        const body = await request.json().catch(() => ({}));
        const validation = validateSessionName(body.name);
        if (!validation.valid) {
            return jsonResponse({ error: validation.error }, 400);
        }
        try {
            const session = await renameSession(db, programId, id, validation.name);
            if (!session) {
                return jsonResponse({ error: "session not found" }, 404);
            }
            return jsonResponse(session);
        } catch (err) {
            if (err instanceof DuplicateNameError) {
                return jsonResponse({ error: err.message }, 409);
            }
            throw err;
        }
    }

    if (request.method === "DELETE") {
        const deleted = await deleteSession(db, programId, id);
        if (!deleted) {
            return jsonResponse({ error: "session not found" }, 404);
        }
        return new Response(null, { status: 204 });
    }

    return new Response("Method not allowed", { status: 405 });
}

// Falls through to the assets binding first (the Blazor static output) rather than a
// hardcoded 404, so a request the platform's own asset routing didn't already
// intercept still gets a real response.
function notFoundOrAssets(request, env) {
    if (env.ASSETS) {
        return env.ASSETS.fetch(request);
    }
    return withCors(new Response("Not found", { status: 404 }), request);
}

// /api/phases (collection, length 2) or /api/phases/:id (resource, length 3)
// -- a flat resource, unlike programs/sessions below.
async function routePhases(request, env, segments) {
    if (segments.length > 3) {
        return notFoundOrAssets(request, env);
    }

    const id = segments[2];
    const response = id
        ? await handlePhaseResource(request, env.DB, id)
        : await handlePhasesCollection(request, env.DB);
    return withCors(response, request);
}

// /api/programs (collection, length 2), /api/programs/:id (resource, length 3),
// /api/programs/:id/sessions (nested collection, length 4), or
// /api/programs/:id/sessions/:sessionId (nested resource, length 5) -- anything else,
// including trailing segments past those, is not a route this function owns, so it
// returns null and the caller falls through to notFoundOrAssets.
async function routePrograms(request, env, segments) {
    const isSessionsRoute =
        segments.length >= 4 && segments.length <= 5 && segments[3] === "sessions";

    if (segments.length > 3 && !isSessionsRoute) {
        return null;
    }

    if (isSessionsRoute) {
        const programId = segments[2];
        const sessionId = segments[4];
        const response = sessionId
            ? await handleSessionResource(request, env.DB, programId, sessionId)
            : await handleSessionsCollection(request, env.DB, programId);
        return withCors(response, request);
    }

    const id = segments[2];
    const response = id
        ? await handleProgramResource(request, env.DB, id)
        : await handleProgramsCollection(request, env.DB);
    return withCors(response, request);
}

export default {
    async fetch(request, env) {
        if (request.method === "OPTIONS") {
            return new Response(null, { status: 204, headers: corsHeadersFor(request) ?? {} });
        }

        const url = new URL(request.url);
        const segments = url.pathname.split("/").filter(Boolean);

        if (segments[0] === "api" && segments[1] === "version" && segments.length === 2) {
            return withCors(handleVersion(request, env), request);
        }

        if (segments[0] === "api" && segments[1] === "phases") {
            return routePhases(request, env, segments);
        }

        if (segments[0] === "api" && segments[1] === "programs") {
            const response = await routePrograms(request, env, segments);
            if (response) {
                return response;
            }
        }

        return notFoundOrAssets(request, env);
    },
};
