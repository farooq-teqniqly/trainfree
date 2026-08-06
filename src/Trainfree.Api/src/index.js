import { createProgram, deleteProgram, listPrograms, renameProgram } from "./programs.js";
import { validateProgramName } from "./validation.js";
import { DuplicateNameError } from "./errors.js";

// The Worker and Blazor client are the same origin in production ([assets] + main share
// one deployment), so these headers never gate a real request there. Locally, Blazor's
// dev server and `wrangler dev` run on different ports -- without CORS the browser
// silently blocks every fetch from the admin UI.
const CORS_HEADERS = {
    "Access-Control-Allow-Origin": "*",
    "Access-Control-Allow-Methods": "GET, POST, PATCH, DELETE, OPTIONS",
    "Access-Control-Allow-Headers": "content-type",
};

function withCors(response) {
    const headers = new Headers(response.headers);
    for (const [key, value] of Object.entries(CORS_HEADERS)) {
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
            return new Response(null, { status: 204, headers: CORS_HEADERS });
        }

        const url = new URL(request.url);
        const segments = url.pathname.split("/").filter(Boolean);

        if (segments[0] !== "api" || segments[1] !== "programs") {
            return withCors(new Response("Not found", { status: 404 }));
        }

        const id = segments[2];
        const response = id
            ? await handleProgramResource(request, env.DB, id)
            : await handleProgramsCollection(request, env.DB);

        return withCors(response);
    },
};
