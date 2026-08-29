import { createProgram, deleteProgram, listPrograms, renameProgram } from "./programs.js";
import { validateProgramName } from "./validation.js";
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

export default {
    async fetch(request, env) {
        if (request.method === "OPTIONS") {
            return new Response(null, { status: 204, headers: corsHeadersFor(request) ?? {} });
        }

        const url = new URL(request.url);
        const segments = url.pathname.split("/").filter(Boolean);

        // Exactly /api/programs (collection, length 2) or /api/programs/:id (resource,
        // length 3) -- anything else, including trailing segments past :id, is not a
        // route this Worker owns. Falls through to the assets binding first (the Blazor
        // static output) rather than a hardcoded 404, so a request the platform's own
        // asset routing didn't already intercept still gets a real response.
        const isProgramsRoute =
            segments[0] === "api" && segments[1] === "programs" && segments.length <= 3;

        if (segments[0] === "api" && segments[1] === "version" && segments.length === 2) {
            return withCors(handleVersion(request, env), request);
        }

        if (!isProgramsRoute) {
            if (env.ASSETS) {
                return env.ASSETS.fetch(request);
            }
            return withCors(new Response("Not found", { status: 404 }), request);
        }

        const id = segments[2];
        const response = id
            ? await handleProgramResource(request, env.DB, id)
            : await handleProgramsCollection(request, env.DB);

        return withCors(response, request);
    },
};
