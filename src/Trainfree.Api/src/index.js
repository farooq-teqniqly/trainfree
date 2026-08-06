import { createProgram, deleteProgram, listPrograms, renameProgram } from "./programs.js";
import { validateProgramName } from "./validation.js";

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
        return jsonResponse(await createProgram(db, validation.name), 201);
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
        const program = await renameProgram(db, id, validation.name);
        if (!program) {
            return jsonResponse({ error: "program not found" }, 404);
        }
        return jsonResponse(program);
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
        const url = new URL(request.url);
        const segments = url.pathname.split("/").filter(Boolean);

        if (segments[0] !== "api" || segments[1] !== "programs") {
            return new Response("Not found", { status: 404 });
        }

        const id = segments[2];

        return id
            ? handleProgramResource(request, env.DB, id)
            : handleProgramsCollection(request, env.DB);
    },
};
