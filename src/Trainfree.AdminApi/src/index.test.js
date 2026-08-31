import { env, SELF } from "cloudflare:test";
import { describe, expect, it } from "vitest";

async function createProgram(name) {
    return SELF.fetch("http://worker/api/programs", {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({ name }),
    });
}

async function createSession(programId, name) {
    return SELF.fetch(`http://worker/api/programs/${programId}/sessions`, {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({ name }),
    });
}

async function createCategory(name) {
    return SELF.fetch("http://worker/api/categories", {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({ name }),
    });
}

describe("CORS", () => {
    it("responds to an OPTIONS preflight from the dev origin with allow headers and no body", async () => {
        const response = await SELF.fetch("http://worker/api/programs", {
            method: "OPTIONS",
            headers: {
                "Access-Control-Request-Method": "POST",
                Origin: "http://localhost:5280",
            },
        });

        expect(response.status).toBe(204);
        expect(response.headers.get("access-control-allow-origin")).toBe(
            "http://localhost:5280",
        );
        expect(response.headers.get("access-control-allow-methods")).toContain("POST");
        expect(await response.text()).toBe("");
    });

    it("includes Access-Control-Allow-Origin for the dev origin on normal responses", async () => {
        const response = await SELF.fetch("http://worker/api/programs", {
            headers: { Origin: "http://localhost:5280" },
        });

        expect(response.headers.get("access-control-allow-origin")).toBe(
            "http://localhost:5280",
        );
    });

    it("omits Access-Control-Allow-Origin when there is no Origin header (same-origin request)", async () => {
        const response = await SELF.fetch("http://worker/api/programs");

        expect(response.headers.has("access-control-allow-origin")).toBe(false);
    });

    it("omits Access-Control-Allow-Origin for an origin other than the known dev origin", async () => {
        const response = await SELF.fetch("http://worker/api/programs", {
            headers: { Origin: "https://evil.example" },
        });

        expect(response.headers.has("access-control-allow-origin")).toBe(false);
    });
});

describe("routing", () => {
    it("returns 404 for a path with segments beyond the resource id", async () => {
        const created = await (await createProgram("Workout A")).json();

        const response = await SELF.fetch(
            `http://worker/api/programs/${created.id}/extra`,
        );

        expect(response.status).toBe(404);
    });

    it("returns 404 for a non-api path when no assets binding is configured", async () => {
        // This test's wrangler.jsonc has no `assets` block (see vitest.config.js), so
        // env.ASSETS is undefined and the Worker falls back to a plain 404 -- the
        // production config (wrangler.deploy.jsonc) has an ASSETS binding instead.
        const response = await SELF.fetch("http://worker/admin");

        expect(response.status).toBe(404);
    });
});

describe("GET /api/programs", () => {
    it("returns an empty array when no programs exist", async () => {
        const response = await SELF.fetch("http://worker/api/programs");

        expect(response.status).toBe(200);
        expect(await response.json()).toEqual([]);
    });

    it("returns programs in creation order", async () => {
        await createProgram("Workout A");
        await createProgram("Workout B");

        const response = await SELF.fetch("http://worker/api/programs");
        const programs = await response.json();

        expect(programs.map((p) => p.name)).toEqual(["Workout A", "Workout B"]);
    });
});

describe("POST /api/programs", () => {
    it("creates a program with a generated id and timestamps", async () => {
        const response = await createProgram("Workout A");
        const program = await response.json();

        expect(response.status).toBe(201);
        expect(program.id).toMatch(/^PRG-[ABCDEFGHJKMNPQRSTVWXYZ23456789]{6}$/);
        expect(program.name).toBe("Workout A");
        expect(program.createdAt).toBeTypeOf("string");
        expect(program.updatedAt).toBeTypeOf("string");
    });

    it("rejects a name that fails the length bound and creates no row", async () => {
        const response = await createProgram("Ab");

        expect(response.status).toBe(400);
        expect((await response.json()).error).toBeTypeOf("string");

        const list = await (await SELF.fetch("http://worker/api/programs")).json();
        expect(list).toHaveLength(0);
    });

    it("rejects a missing name", async () => {
        const response = await SELF.fetch("http://worker/api/programs", {
            method: "POST",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({}),
        });

        expect(response.status).toBe(400);
    });

    it("rejects a name that already exists, case-insensitively, and creates no row", async () => {
        await createProgram("Workout A");

        const response = await createProgram("workout a");

        expect(response.status).toBe(409);
        expect((await response.json()).error).toBeTypeOf("string");

        const list = await (await SELF.fetch("http://worker/api/programs")).json();
        expect(list).toHaveLength(1);
    });
});

describe("PATCH /api/programs/:id", () => {
    it("renames an existing program", async () => {
        const created = await (await createProgram("Workout A")).json();

        const response = await SELF.fetch(`http://worker/api/programs/${created.id}`, {
            method: "PATCH",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ name: "Renamed Workout" }),
        });
        const program = await response.json();

        expect(response.status).toBe(200);
        expect(program.name).toBe("Renamed Workout");
        expect(program.id).toBe(created.id);
    });

    it("returns 404 for an unknown id", async () => {
        const response = await SELF.fetch("http://worker/api/programs/PRG-ZZZZZZ", {
            method: "PATCH",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ name: "Renamed Workout" }),
        });

        expect(response.status).toBe(404);
    });

    it("rejects a name that fails the length bound and makes no change", async () => {
        const created = await (await createProgram("Workout A")).json();

        const response = await SELF.fetch(`http://worker/api/programs/${created.id}`, {
            method: "PATCH",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ name: "Ab" }),
        });

        expect(response.status).toBe(400);

        const list = await (await SELF.fetch("http://worker/api/programs")).json();
        expect(list[0].name).toBe("Workout A");
    });

    it("rejects renaming to another program's name, case-insensitively, and makes no change", async () => {
        await createProgram("Workout A");
        const other = await (await createProgram("Workout B")).json();

        const response = await SELF.fetch(`http://worker/api/programs/${other.id}`, {
            method: "PATCH",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ name: "workout a" }),
        });

        expect(response.status).toBe(409);

        const list = await (await SELF.fetch("http://worker/api/programs")).json();
        expect(list.map((p) => p.name)).toEqual(["Workout A", "Workout B"]);
    });

    it("allows renaming a program to its own current name", async () => {
        const created = await (await createProgram("Workout A")).json();

        const response = await SELF.fetch(`http://worker/api/programs/${created.id}`, {
            method: "PATCH",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ name: "Workout A" }),
        });

        expect(response.status).toBe(200);
    });
});

describe("DELETE /api/programs/:id", () => {
    it("deletes an existing program", async () => {
        const created = await (await createProgram("Workout A")).json();

        const response = await SELF.fetch(`http://worker/api/programs/${created.id}`, {
            method: "DELETE",
        });

        expect(response.status).toBe(204);

        const list = await (await SELF.fetch("http://worker/api/programs")).json();
        expect(list).toHaveLength(0);
    });

    it("returns 404 for an unknown id", async () => {
        const response = await SELF.fetch("http://worker/api/programs/PRG-ZZZZZZ", {
            method: "DELETE",
        });

        expect(response.status).toBe(404);
    });
});

describe("GET /api/programs with multiple rows", () => {
    it("returns all programs in creation order", async () => {
        await createProgram("Workout A");
        await createProgram("Workout B");
        await createProgram("Workout C");

        const response = await SELF.fetch("http://worker/api/programs");
        const programs = await response.json();

        expect(programs.map((p) => p.name)).toEqual(["Workout A", "Workout B", "Workout C"]);
    });
});

describe("GET /api/programs/:programId/sessions", () => {
    it("returns an empty array when the program has no sessions", async () => {
        const program = await (await createProgram("Workout A")).json();

        const response = await SELF.fetch(
            `http://worker/api/programs/${program.id}/sessions`,
        );

        expect(response.status).toBe(200);
        expect(await response.json()).toEqual([]);
    });

    it("returns the program's sessions in creation order", async () => {
        const program = await (await createProgram("Workout A")).json();
        await createSession(program.id, "Monday Lower Body");
        await createSession(program.id, "Wednesday Upper Body");

        const response = await SELF.fetch(
            `http://worker/api/programs/${program.id}/sessions`,
        );
        const sessions = await response.json();

        expect(sessions.map((s) => s.name)).toEqual([
            "Monday Lower Body",
            "Wednesday Upper Body",
        ]);
    });

    it("breaks a created_at tie using insertion order", async () => {
        const program = await (await createProgram("Workout A")).json();
        const tiedTimestamp = new Date().toISOString();
        await env.DB.prepare(
            "INSERT INTO sessions (session_id, program_id, name, created_at, updated_at) VALUES (?, ?, ?, ?, ?)",
        )
            .bind("SNN-ZZZZZZ", program.id, "Inserted First", tiedTimestamp, tiedTimestamp)
            .run();
        await env.DB.prepare(
            "INSERT INTO sessions (session_id, program_id, name, created_at, updated_at) VALUES (?, ?, ?, ?, ?)",
        )
            .bind("SNN-AAAAAA", program.id, "Inserted Second", tiedTimestamp, tiedTimestamp)
            .run();

        const response = await SELF.fetch(
            `http://worker/api/programs/${program.id}/sessions`,
        );
        const sessions = await response.json();

        expect(sessions.map((s) => s.name)).toEqual(["Inserted First", "Inserted Second"]);
    });

    it("excludes sessions belonging to a different program", async () => {
        const programA = await (await createProgram("Workout A")).json();
        const programB = await (await createProgram("Workout B")).json();
        await createSession(programA.id, "Monday Lower Body");
        await createSession(programB.id, "Tuesday Upper Body");

        const response = await SELF.fetch(
            `http://worker/api/programs/${programA.id}/sessions`,
        );
        const sessions = await response.json();

        expect(sessions.map((s) => s.name)).toEqual(["Monday Lower Body"]);
    });

    it("returns 404 for an unknown programId", async () => {
        const response = await SELF.fetch(
            "http://worker/api/programs/PRG-ZZZZZZ/sessions",
        );

        expect(response.status).toBe(404);
    });
});

describe("POST /api/programs/:programId/sessions", () => {
    it("creates a session with a generated id and timestamps", async () => {
        const program = await (await createProgram("Workout A")).json();

        const response = await createSession(program.id, "Monday Lower Body");
        const session = await response.json();

        expect(response.status).toBe(201);
        expect(session.id).toMatch(/^SNN-[ABCDEFGHJKMNPQRSTVWXYZ23456789]{6}$/);
        expect(session.programId).toBe(program.id);
        expect(session.name).toBe("Monday Lower Body");
        expect(session.createdAt).toBeTypeOf("string");
        expect(session.updatedAt).toBeTypeOf("string");
    });

    it("rejects a name that fails the length bound and creates no row", async () => {
        const program = await (await createProgram("Workout A")).json();

        const response = await createSession(program.id, "Ab");

        expect(response.status).toBe(400);
        expect((await response.json()).error).toBeTypeOf("string");

        const list = await (
            await SELF.fetch(`http://worker/api/programs/${program.id}/sessions`)
        ).json();
        expect(list).toHaveLength(0);
    });

    it("rejects a name that already exists in the same program, case-insensitively", async () => {
        const program = await (await createProgram("Workout A")).json();
        await createSession(program.id, "Monday Lower Body");

        const response = await createSession(program.id, "monday lower body");

        expect(response.status).toBe(409);
        expect((await response.json()).error).toBeTypeOf("string");

        const list = await (
            await SELF.fetch(`http://worker/api/programs/${program.id}/sessions`)
        ).json();
        expect(list).toHaveLength(1);
    });

    it("allows the same name in a different program", async () => {
        const programA = await (await createProgram("Workout A")).json();
        const programB = await (await createProgram("Workout B")).json();
        await createSession(programA.id, "Monday Lower Body");

        const response = await createSession(programB.id, "Monday Lower Body");

        expect(response.status).toBe(201);
    });

    it("returns 404 for an unknown programId and creates no row", async () => {
        const response = await createSession("PRG-ZZZZZZ", "Monday Lower Body");

        expect(response.status).toBe(404);
    });
});

describe("PATCH /api/programs/:programId/sessions/:id", () => {
    it("renames an existing session", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();

        const response = await SELF.fetch(
            `http://worker/api/programs/${program.id}/sessions/${session.id}`,
            {
                method: "PATCH",
                headers: { "content-type": "application/json" },
                body: JSON.stringify({ name: "Renamed Session" }),
            },
        );
        const updated = await response.json();

        expect(response.status).toBe(200);
        expect(updated.name).toBe("Renamed Session");
        expect(updated.id).toBe(session.id);
    });

    it("returns 404 for an unknown session id", async () => {
        const program = await (await createProgram("Workout A")).json();

        const response = await SELF.fetch(
            `http://worker/api/programs/${program.id}/sessions/SNN-ZZZZZZ`,
            {
                method: "PATCH",
                headers: { "content-type": "application/json" },
                body: JSON.stringify({ name: "Renamed Session" }),
            },
        );

        expect(response.status).toBe(404);
    });

    it("returns 404 when the session belongs to a different program", async () => {
        const programA = await (await createProgram("Workout A")).json();
        const programB = await (await createProgram("Workout B")).json();
        const session = await (
            await createSession(programA.id, "Monday Lower Body")
        ).json();

        const response = await SELF.fetch(
            `http://worker/api/programs/${programB.id}/sessions/${session.id}`,
            {
                method: "PATCH",
                headers: { "content-type": "application/json" },
                body: JSON.stringify({ name: "Renamed Session" }),
            },
        );

        expect(response.status).toBe(404);
    });

    it("rejects a name that fails the length bound and makes no change", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();

        const response = await SELF.fetch(
            `http://worker/api/programs/${program.id}/sessions/${session.id}`,
            {
                method: "PATCH",
                headers: { "content-type": "application/json" },
                body: JSON.stringify({ name: "Ab" }),
            },
        );

        expect(response.status).toBe(400);

        const list = await (
            await SELF.fetch(`http://worker/api/programs/${program.id}/sessions`)
        ).json();
        expect(list[0].name).toBe("Monday Lower Body");
    });

    it("rejects renaming to another session's name in the same program, case-insensitively", async () => {
        const program = await (await createProgram("Workout A")).json();
        await createSession(program.id, "Monday Lower Body");
        const other = await (
            await createSession(program.id, "Wednesday Upper Body")
        ).json();

        const response = await SELF.fetch(
            `http://worker/api/programs/${program.id}/sessions/${other.id}`,
            {
                method: "PATCH",
                headers: { "content-type": "application/json" },
                body: JSON.stringify({ name: "monday lower body" }),
            },
        );

        expect(response.status).toBe(409);

        const list = await (
            await SELF.fetch(`http://worker/api/programs/${program.id}/sessions`)
        ).json();
        expect(list.map((s) => s.name)).toEqual([
            "Monday Lower Body",
            "Wednesday Upper Body",
        ]);
    });

    it("allows renaming a session to its own current name", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();

        const response = await SELF.fetch(
            `http://worker/api/programs/${program.id}/sessions/${session.id}`,
            {
                method: "PATCH",
                headers: { "content-type": "application/json" },
                body: JSON.stringify({ name: "Monday Lower Body" }),
            },
        );

        expect(response.status).toBe(200);
    });
});

describe("DELETE /api/programs/:programId/sessions/:id", () => {
    it("deletes an existing session", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();

        const response = await SELF.fetch(
            `http://worker/api/programs/${program.id}/sessions/${session.id}`,
            { method: "DELETE" },
        );

        expect(response.status).toBe(204);

        const list = await (
            await SELF.fetch(`http://worker/api/programs/${program.id}/sessions`)
        ).json();
        expect(list).toHaveLength(0);
    });

    it("returns 404 for an unknown session id", async () => {
        const program = await (await createProgram("Workout A")).json();

        const response = await SELF.fetch(
            `http://worker/api/programs/${program.id}/sessions/SNN-ZZZZZZ`,
            { method: "DELETE" },
        );

        expect(response.status).toBe(404);
    });

    it("returns 404 when the session belongs to a different program", async () => {
        const programA = await (await createProgram("Workout A")).json();
        const programB = await (await createProgram("Workout B")).json();
        const session = await (
            await createSession(programA.id, "Monday Lower Body")
        ).json();

        const response = await SELF.fetch(
            `http://worker/api/programs/${programB.id}/sessions/${session.id}`,
            { method: "DELETE" },
        );

        expect(response.status).toBe(404);
    });
});

describe("GET /api/categories", () => {
    it("returns an empty array when no categories exist", async () => {
        const response = await SELF.fetch("http://worker/api/categories");

        expect(response.status).toBe(200);
        expect(await response.json()).toEqual([]);
    });

    it("returns categories in creation order", async () => {
        await createCategory("Warm Up");
        await createCategory("Cool Down");

        const response = await SELF.fetch("http://worker/api/categories");
        const categories = await response.json();

        expect(categories.map((c) => c.name)).toEqual(["Warm Up", "Cool Down"]);
    });
});

describe("POST /api/categories", () => {
    it("creates a category with a generated id and timestamps", async () => {
        const response = await createCategory("Warm Up");
        const category = await response.json();

        expect(response.status).toBe(201);
        expect(category.id).toMatch(/^CAT-[ABCDEFGHJKMNPQRSTVWXYZ23456789]{6}$/);
        expect(category.name).toBe("Warm Up");
        expect(category.createdAt).toBeTypeOf("string");
        expect(category.updatedAt).toBeTypeOf("string");
    });

    it("rejects a name that fails the length bound and creates no row", async () => {
        const response = await createCategory("Ab");

        expect(response.status).toBe(400);
        expect((await response.json()).error).toBeTypeOf("string");

        const list = await (await SELF.fetch("http://worker/api/categories")).json();
        expect(list).toHaveLength(0);
    });

    it("rejects a missing name", async () => {
        const response = await SELF.fetch("http://worker/api/categories", {
            method: "POST",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({}),
        });

        expect(response.status).toBe(400);
    });

    it("rejects a name that already exists, case-insensitively, and creates no row", async () => {
        await createCategory("Warm Up");

        const response = await createCategory("warm up");

        expect(response.status).toBe(409);
        expect((await response.json()).error).toBeTypeOf("string");

        const list = await (await SELF.fetch("http://worker/api/categories")).json();
        expect(list).toHaveLength(1);
    });
});

describe("PATCH /api/categories/:id", () => {
    it("renames an existing category", async () => {
        const created = await (await createCategory("Warm Up")).json();

        const response = await SELF.fetch(`http://worker/api/categories/${created.id}`, {
            method: "PATCH",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ name: "Cool Down" }),
        });
        const category = await response.json();

        expect(response.status).toBe(200);
        expect(category.name).toBe("Cool Down");
        expect(category.id).toBe(created.id);
    });

    it("returns 404 for an unknown id", async () => {
        const response = await SELF.fetch("http://worker/api/categories/CAT-ZZZZZZ", {
            method: "PATCH",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ name: "Cool Down" }),
        });

        expect(response.status).toBe(404);
    });

    it("rejects a name that fails the length bound and makes no change", async () => {
        const created = await (await createCategory("Warm Up")).json();

        const response = await SELF.fetch(`http://worker/api/categories/${created.id}`, {
            method: "PATCH",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ name: "Ab" }),
        });

        expect(response.status).toBe(400);

        const list = await (await SELF.fetch("http://worker/api/categories")).json();
        expect(list[0].name).toBe("Warm Up");
    });

    it("rejects renaming to another category's name, case-insensitively, and makes no change", async () => {
        await createCategory("Warm Up");
        const other = await (await createCategory("Cool Down")).json();

        const response = await SELF.fetch(`http://worker/api/categories/${other.id}`, {
            method: "PATCH",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ name: "warm up" }),
        });

        expect(response.status).toBe(409);

        const list = await (await SELF.fetch("http://worker/api/categories")).json();
        expect(list.map((c) => c.name)).toEqual(["Warm Up", "Cool Down"]);
    });

    it("allows renaming a category to its own current name", async () => {
        const created = await (await createCategory("Warm Up")).json();

        const response = await SELF.fetch(`http://worker/api/categories/${created.id}`, {
            method: "PATCH",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ name: "Warm Up" }),
        });

        expect(response.status).toBe(200);
    });
});

describe("DELETE /api/categories/:id", () => {
    it("deletes an existing category", async () => {
        const created = await (await createCategory("Warm Up")).json();

        const response = await SELF.fetch(`http://worker/api/categories/${created.id}`, {
            method: "DELETE",
        });

        expect(response.status).toBe(204);

        const list = await (await SELF.fetch("http://worker/api/categories")).json();
        expect(list).toHaveLength(0);
    });

    it("returns 404 for an unknown id", async () => {
        const response = await SELF.fetch("http://worker/api/categories/CAT-ZZZZZZ", {
            method: "DELETE",
        });

        expect(response.status).toBe(404);
    });
});

describe("DELETE /api/programs/:id cascades to sessions", () => {
    it("removes a deleted program's sessions", async () => {
        const program = await (await createProgram("Workout A")).json();
        await createSession(program.id, "Monday Lower Body");
        await createSession(program.id, "Wednesday Upper Body");

        const response = await SELF.fetch(`http://worker/api/programs/${program.id}`, {
            method: "DELETE",
        });

        expect(response.status).toBe(204);

        const listResponse = await SELF.fetch(
            `http://worker/api/programs/${program.id}/sessions`,
        );
        // The program no longer exists, so its (now cascade-deleted) sessions are
        // unreachable via a 404 rather than an empty list -- this also proves the
        // rows were actually removed by D1's FK cascade, not just orphaned.
        expect(listResponse.status).toBe(404);
    });
});
