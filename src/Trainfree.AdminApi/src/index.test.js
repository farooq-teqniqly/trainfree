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

async function createPhase(name) {
    return SELF.fetch("http://worker/api/phases", {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({ name }),
    });
}

async function createExercise(name) {
    return SELF.fetch("http://worker/api/exercises", {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({ name }),
    });
}

async function createSessionPhase(programId, sessionId, phaseId) {
    return SELF.fetch(
        `http://worker/api/programs/${programId}/sessions/${sessionId}/phases`,
        {
            method: "POST",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ phaseId }),
        },
    );
}

function programExercisesUrl(programId, sessionId, sessionPhaseId, id) {
    const base = `http://worker/api/programs/${programId}/sessions/${sessionId}/phases/${sessionPhaseId}/exercises`;
    return id ? `${base}/${id}` : base;
}

async function createProgramExercise(programId, sessionId, sessionPhaseId, body) {
    return SELF.fetch(programExercisesUrl(programId, sessionId, sessionPhaseId), {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify(body),
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

    // RED-phase note: `programs.id` is an AUTOINCREMENT rowid alias, so internal id
    // order is always identical to insertion order -- a local SQLite/Miniflare rerun
    // with the `programs.id ASC` tiebreak removed still passes this test, because the
    // engine's scan-then-sort happens to preserve insertion order regardless. This was
    // manually verified (see PR #72 review, issue #53): reverting the ORDER BY clause
    // does not fail this test locally. The assertion still documents and pins the
    // contractual behavior (SQL gives no ordering guarantee on ties without an
    // explicit tiebreak column), it just can't be proven RED against this engine.
    it("breaks a created_at tie using insertion order", async () => {
        const tiedTimestamp = new Date().toISOString();
        await env.DB.prepare(
            "INSERT INTO programs (program_id, name, created_at, updated_at) VALUES (?, ?, ?, ?)",
        )
            .bind("PRG-ZZZZZZ", "Inserted First", tiedTimestamp, tiedTimestamp)
            .run();
        await env.DB.prepare(
            "INSERT INTO programs (program_id, name, created_at, updated_at) VALUES (?, ?, ?, ?)",
        )
            .bind("PRG-AAAAAA", "Inserted Second", tiedTimestamp, tiedTimestamp)
            .run();

        const response = await SELF.fetch("http://worker/api/programs");
        const programs = await response.json();

        expect(programs.map((p) => p.name)).toEqual(["Inserted First", "Inserted Second"]);
    });
});

describe("listPrograms SQL", () => {
    it("qualifies the id tiebreaker to the programs table", async () => {
        const { LIST_PROGRAMS_QUERY } = await import("./programs.js");

        expect(LIST_PROGRAMS_QUERY).toMatch(/order by created_at asc, programs\.id asc/i);
    });
});

describe("listPhases SQL", () => {
    it("qualifies the id tiebreaker to the phases table", async () => {
        const { LIST_PHASES_QUERY } = await import("./phases.js");

        expect(LIST_PHASES_QUERY).toMatch(/order by created_at asc, phases\.id asc/i);
    });
});

describe("listExercises SQL", () => {
    it("qualifies the id tiebreaker to the exercises table", async () => {
        const { LIST_EXERCISES_QUERY } = await import("./exercises.js");

        expect(LIST_EXERCISES_QUERY).toMatch(
            /order by created_at asc, exercises\.id asc/i,
        );
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

    // RED-phase note: see the identical caveat on the programs/phases tiebreak tests
    // in this file -- sessions.id is also an AUTOINCREMENT rowid alias, so this can't
    // be proven RED locally either. This test predates PR #72 (#43); the caveat
    // applies unchanged.
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

describe("GET /api/programs/:programId/sessions/:sessionId/phases", () => {
    it("returns an empty array when the session has no phases", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();

        const response = await SELF.fetch(
            `http://worker/api/programs/${program.id}/sessions/${session.id}/phases`,
        );

        expect(response.status).toBe(200);
        expect(await response.json()).toEqual([]);
    });

    it("returns the session's phases", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();
        const phase = await (await createPhase("Warm Up")).json();
        await createSessionPhase(program.id, session.id, phase.id);

        const response = await SELF.fetch(
            `http://worker/api/programs/${program.id}/sessions/${session.id}/phases`,
        );
        const sessionPhases = await response.json();

        expect(sessionPhases).toHaveLength(1);
        expect(sessionPhases[0].phaseId).toBe(phase.id);
    });

    it("returns 404 for an unknown sessionId", async () => {
        const program = await (await createProgram("Workout A")).json();

        const response = await SELF.fetch(
            `http://worker/api/programs/${program.id}/sessions/SNN-ZZZZZZ/phases`,
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
            `http://worker/api/programs/${programB.id}/sessions/${session.id}/phases`,
        );

        expect(response.status).toBe(404);
    });
});

describe("POST /api/programs/:programId/sessions/:sessionId/phases", () => {
    it("creates a session phase with a generated id", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();
        const phase = await (await createPhase("Warm Up")).json();

        const response = await createSessionPhase(program.id, session.id, phase.id);

        expect(response.status).toBe(201);
        const created = await response.json();
        expect(created.phaseId).toBe(phase.id);
        expect(created.sessionId).toBe(session.id);
    });

    it("returns 404 for an unknown sessionId", async () => {
        const program = await (await createProgram("Workout A")).json();
        const phase = await (await createPhase("Warm Up")).json();

        const response = await createSessionPhase(program.id, "SNN-ZZZZZZ", phase.id);

        expect(response.status).toBe(404);
    });

    it("returns 400 for a missing phaseId", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();

        const response = await SELF.fetch(
            `http://worker/api/programs/${program.id}/sessions/${session.id}/phases`,
            {
                method: "POST",
                headers: { "content-type": "application/json" },
                body: JSON.stringify({}),
            },
        );

        expect(response.status).toBe(400);
    });

    it("returns 400 for a phaseId that matches no phase", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();

        const response = await createSessionPhase(program.id, session.id, "PHS-ZZZZZZ");

        expect(response.status).toBe(400);
    });

    it("rejects a literal JSON null body instead of throwing", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();

        const response = await SELF.fetch(
            `http://worker/api/programs/${program.id}/sessions/${session.id}/phases`,
            {
                method: "POST",
                headers: { "content-type": "application/json" },
                body: "null",
            },
        );

        expect(response.status).toBe(400);
    });

    it("allows adding the same phase to a session twice", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();
        const phase = await (await createPhase("Warm Up")).json();
        await createSessionPhase(program.id, session.id, phase.id);

        const response = await createSessionPhase(program.id, session.id, phase.id);

        expect(response.status).toBe(201);
        const list = await (
            await SELF.fetch(
                `http://worker/api/programs/${program.id}/sessions/${session.id}/phases`,
            )
        ).json();
        expect(list).toHaveLength(2);
    });
});

describe("DELETE /api/programs/:programId/sessions/:sessionId/phases/:id", () => {
    it("deletes an existing session phase", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();
        const phase = await (await createPhase("Warm Up")).json();
        const sessionPhase = await (
            await createSessionPhase(program.id, session.id, phase.id)
        ).json();

        const response = await SELF.fetch(
            `http://worker/api/programs/${program.id}/sessions/${session.id}/phases/${sessionPhase.id}`,
            { method: "DELETE" },
        );

        expect(response.status).toBe(204);
        const list = await (
            await SELF.fetch(
                `http://worker/api/programs/${program.id}/sessions/${session.id}/phases`,
            )
        ).json();
        expect(list).toHaveLength(0);
    });

    it("returns 404 for an unknown id", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();

        const response = await SELF.fetch(
            `http://worker/api/programs/${program.id}/sessions/${session.id}/phases/SPH-ZZZZZZ`,
            { method: "DELETE" },
        );

        expect(response.status).toBe(404);
    });

    it("returns 404 when the session phase belongs to a different session", async () => {
        const program = await (await createProgram("Workout A")).json();
        const sessionA = await (await createSession(program.id, "Monday Lower Body")).json();
        const sessionB = await (
            await createSession(program.id, "Wednesday Upper Body")
        ).json();
        const phase = await (await createPhase("Warm Up")).json();
        const sessionPhase = await (
            await createSessionPhase(program.id, sessionA.id, phase.id)
        ).json();

        const response = await SELF.fetch(
            `http://worker/api/programs/${program.id}/sessions/${sessionB.id}/phases/${sessionPhase.id}`,
            { method: "DELETE" },
        );

        expect(response.status).toBe(404);
    });
});

describe("DELETE /api/programs/:programId/sessions/:id cascades to session phases", () => {
    it("removes a deleted session's session phases", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();
        const phase = await (await createPhase("Warm Up")).json();
        await createSessionPhase(program.id, session.id, phase.id);

        const deleteResponse = await SELF.fetch(
            `http://worker/api/programs/${program.id}/sessions/${session.id}`,
            { method: "DELETE" },
        );
        expect(deleteResponse.status).toBe(204);

        const listResponse = await SELF.fetch(
            `http://worker/api/programs/${program.id}/sessions/${session.id}/phases`,
        );
        // The session no longer exists, so its (now cascade-deleted) session phases are
        // unreachable via a 404 rather than an empty list -- this also proves the rows
        // were actually removed by D1's FK cascade, not just orphaned.
        expect(listResponse.status).toBe(404);
    });
});

describe("GET /api/phases", () => {
    it("returns an empty array when no phases exist", async () => {
        const response = await SELF.fetch("http://worker/api/phases");

        expect(response.status).toBe(200);
        expect(await response.json()).toEqual([]);
    });

    it("returns phases in creation order", async () => {
        await createPhase("Warm Up");
        await createPhase("Cool Down");

        const response = await SELF.fetch("http://worker/api/phases");
        const phases = await response.json();

        expect(phases.map((p) => p.name)).toEqual(["Warm Up", "Cool Down"]);
    });

    // RED-phase note: see the identical caveat on the programs test above -- `phases.id`
    // is also an AUTOINCREMENT rowid alias, so this can't be proven RED locally either.
    it("breaks a created_at tie using insertion order", async () => {
        const tiedTimestamp = new Date().toISOString();
        await env.DB.prepare(
            "INSERT INTO phases (phase_id, name, created_at, updated_at) VALUES (?, ?, ?, ?)",
        )
            .bind("PHS-ZZZZZZ", "Inserted First", tiedTimestamp, tiedTimestamp)
            .run();
        await env.DB.prepare(
            "INSERT INTO phases (phase_id, name, created_at, updated_at) VALUES (?, ?, ?, ?)",
        )
            .bind("PHS-AAAAAA", "Inserted Second", tiedTimestamp, tiedTimestamp)
            .run();

        const response = await SELF.fetch("http://worker/api/phases");
        const phases = await response.json();

        expect(phases.map((p) => p.name)).toEqual(["Inserted First", "Inserted Second"]);
    });
});

describe("POST /api/phases", () => {
    it("creates a phase with a generated id and timestamps", async () => {
        const response = await createPhase("Warm Up");
        const phase = await response.json();

        expect(response.status).toBe(201);
        expect(phase.id).toMatch(/^PHS-[ABCDEFGHJKMNPQRSTVWXYZ23456789]{6}$/);
        expect(phase.name).toBe("Warm Up");
        expect(phase.createdAt).toBeTypeOf("string");
        expect(phase.updatedAt).toBeTypeOf("string");
    });

    it("rejects a name that fails the length bound and creates no row", async () => {
        const response = await createPhase("Ab");

        expect(response.status).toBe(400);
        expect((await response.json()).error).toBeTypeOf("string");

        const list = await (await SELF.fetch("http://worker/api/phases")).json();
        expect(list).toHaveLength(0);
    });

    it("rejects a missing name", async () => {
        const response = await SELF.fetch("http://worker/api/phases", {
            method: "POST",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({}),
        });

        expect(response.status).toBe(400);
    });

    it("rejects a name that already exists, case-insensitively, and creates no row", async () => {
        await createPhase("Warm Up");

        const response = await createPhase("warm up");

        expect(response.status).toBe(409);
        expect((await response.json()).error).toBeTypeOf("string");

        const list = await (await SELF.fetch("http://worker/api/phases")).json();
        expect(list).toHaveLength(1);
    });
});

describe("PATCH /api/phases/:id", () => {
    it("renames an existing phase", async () => {
        const created = await (await createPhase("Warm Up")).json();

        const response = await SELF.fetch(`http://worker/api/phases/${created.id}`, {
            method: "PATCH",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ name: "Cool Down" }),
        });
        const phase = await response.json();

        expect(response.status).toBe(200);
        expect(phase.name).toBe("Cool Down");
        expect(phase.id).toBe(created.id);
    });

    it("returns 404 for an unknown id", async () => {
        const response = await SELF.fetch("http://worker/api/phases/PHS-ZZZZZZ", {
            method: "PATCH",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ name: "Cool Down" }),
        });

        expect(response.status).toBe(404);
    });

    it("rejects a name that fails the length bound and makes no change", async () => {
        const created = await (await createPhase("Warm Up")).json();

        const response = await SELF.fetch(`http://worker/api/phases/${created.id}`, {
            method: "PATCH",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ name: "Ab" }),
        });

        expect(response.status).toBe(400);

        const list = await (await SELF.fetch("http://worker/api/phases")).json();
        expect(list[0].name).toBe("Warm Up");
    });

    it("rejects renaming to another phase's name, case-insensitively, and makes no change", async () => {
        await createPhase("Warm Up");
        const other = await (await createPhase("Cool Down")).json();

        const response = await SELF.fetch(`http://worker/api/phases/${other.id}`, {
            method: "PATCH",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ name: "warm up" }),
        });

        expect(response.status).toBe(409);

        const list = await (await SELF.fetch("http://worker/api/phases")).json();
        expect(list.map((p) => p.name)).toEqual(["Warm Up", "Cool Down"]);
    });

    it("allows renaming a phase to its own current name", async () => {
        const created = await (await createPhase("Warm Up")).json();

        const response = await SELF.fetch(`http://worker/api/phases/${created.id}`, {
            method: "PATCH",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ name: "Warm Up" }),
        });

        expect(response.status).toBe(200);
    });
});

describe("DELETE /api/phases/:id", () => {
    it("deletes an existing phase", async () => {
        const created = await (await createPhase("Warm Up")).json();

        const response = await SELF.fetch(`http://worker/api/phases/${created.id}`, {
            method: "DELETE",
        });

        expect(response.status).toBe(204);

        const list = await (await SELF.fetch("http://worker/api/phases")).json();
        expect(list).toHaveLength(0);
    });

    it("returns 404 for an unknown id", async () => {
        const response = await SELF.fetch("http://worker/api/phases/PHS-ZZZZZZ", {
            method: "DELETE",
        });

        expect(response.status).toBe(404);
    });

    it("returns 409 and makes no change when the phase is referenced by a session", async () => {
        const phase = await (await createPhase("Warm Up")).json();
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();
        await createSessionPhase(program.id, session.id, phase.id);

        const response = await SELF.fetch(`http://worker/api/phases/${phase.id}`, {
            method: "DELETE",
        });

        expect(response.status).toBe(409);
        const list = await (await SELF.fetch("http://worker/api/phases")).json();
        expect(list).toHaveLength(1);
    });
});

describe("GET /api/exercises", () => {
    it("returns an empty array when no exercises exist", async () => {
        const response = await SELF.fetch("http://worker/api/exercises");

        expect(response.status).toBe(200);
        expect(await response.json()).toEqual([]);
    });

    it("returns exercises in creation order", async () => {
        await createExercise("Bodyweight Squat");
        await createExercise("Skater Jump");

        const response = await SELF.fetch("http://worker/api/exercises");
        const exercises = await response.json();

        expect(exercises.map((e) => e.name)).toEqual(["Bodyweight Squat", "Skater Jump"]);
    });

    // RED-phase note: see the identical caveat on the phases test above -- `exercises.id`
    // is also an AUTOINCREMENT rowid alias, so this can't be proven RED locally either.
    it("breaks a created_at tie using insertion order", async () => {
        const tiedTimestamp = new Date().toISOString();
        await env.DB.prepare(
            "INSERT INTO exercises (exercise_id, name, created_at, updated_at) VALUES (?, ?, ?, ?)",
        )
            .bind("EXR-ZZZZZZ", "Inserted First", tiedTimestamp, tiedTimestamp)
            .run();
        await env.DB.prepare(
            "INSERT INTO exercises (exercise_id, name, created_at, updated_at) VALUES (?, ?, ?, ?)",
        )
            .bind("EXR-AAAAAA", "Inserted Second", tiedTimestamp, tiedTimestamp)
            .run();

        const response = await SELF.fetch("http://worker/api/exercises");
        const exercises = await response.json();

        expect(exercises.map((e) => e.name)).toEqual(["Inserted First", "Inserted Second"]);
    });
});

describe("POST /api/exercises", () => {
    it("creates an exercise with a generated id and timestamps", async () => {
        const response = await createExercise("Bodyweight Squat");
        const exercise = await response.json();

        expect(response.status).toBe(201);
        expect(exercise.id).toMatch(/^EXR-[ABCDEFGHJKMNPQRSTVWXYZ23456789]{6}$/);
        expect(exercise.name).toBe("Bodyweight Squat");
        expect(exercise.createdAt).toBeTypeOf("string");
        expect(exercise.updatedAt).toBeTypeOf("string");
    });

    it("rejects a name that fails the length bound and creates no row", async () => {
        const response = await createExercise("Ab");

        expect(response.status).toBe(400);
        expect((await response.json()).error).toBeTypeOf("string");

        const list = await (await SELF.fetch("http://worker/api/exercises")).json();
        expect(list).toHaveLength(0);
    });

    it("rejects a missing name", async () => {
        const response = await SELF.fetch("http://worker/api/exercises", {
            method: "POST",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({}),
        });

        expect(response.status).toBe(400);
    });

    it("rejects a literal JSON null body instead of throwing", async () => {
        const response = await SELF.fetch("http://worker/api/exercises", {
            method: "POST",
            headers: { "content-type": "application/json" },
            body: "null",
        });

        expect(response.status).toBe(400);
    });

    it("rejects a name that already exists, case-insensitively, and creates no row", async () => {
        await createExercise("Bodyweight Squat");

        const response = await createExercise("bodyweight squat");

        expect(response.status).toBe(409);
        expect((await response.json()).error).toBeTypeOf("string");

        const list = await (await SELF.fetch("http://worker/api/exercises")).json();
        expect(list).toHaveLength(1);
    });
});

describe("PATCH /api/exercises/:id", () => {
    it("renames an existing exercise", async () => {
        const created = await (await createExercise("Bodyweight Squat")).json();

        const response = await SELF.fetch(`http://worker/api/exercises/${created.id}`, {
            method: "PATCH",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ name: "Skater Jump" }),
        });
        const exercise = await response.json();

        expect(response.status).toBe(200);
        expect(exercise.name).toBe("Skater Jump");
        expect(exercise.id).toBe(created.id);
    });

    it("returns 404 for an unknown id", async () => {
        const response = await SELF.fetch("http://worker/api/exercises/EXR-ZZZZZZ", {
            method: "PATCH",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ name: "Skater Jump" }),
        });

        expect(response.status).toBe(404);
    });

    it("rejects a name that fails the length bound and makes no change", async () => {
        const created = await (await createExercise("Bodyweight Squat")).json();

        const response = await SELF.fetch(`http://worker/api/exercises/${created.id}`, {
            method: "PATCH",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ name: "Ab" }),
        });

        expect(response.status).toBe(400);

        const list = await (await SELF.fetch("http://worker/api/exercises")).json();
        expect(list[0].name).toBe("Bodyweight Squat");
    });

    it("rejects a literal JSON null body instead of throwing", async () => {
        const created = await (await createExercise("Bodyweight Squat")).json();

        const response = await SELF.fetch(`http://worker/api/exercises/${created.id}`, {
            method: "PATCH",
            headers: { "content-type": "application/json" },
            body: "null",
        });

        expect(response.status).toBe(400);
    });

    it("rejects renaming to another exercise's name, case-insensitively, and makes no change", async () => {
        await createExercise("Bodyweight Squat");
        const other = await (await createExercise("Skater Jump")).json();

        const response = await SELF.fetch(`http://worker/api/exercises/${other.id}`, {
            method: "PATCH",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ name: "bodyweight squat" }),
        });

        expect(response.status).toBe(409);

        const list = await (await SELF.fetch("http://worker/api/exercises")).json();
        expect(list.map((e) => e.name)).toEqual(["Bodyweight Squat", "Skater Jump"]);
    });

    it("allows renaming an exercise to its own current name", async () => {
        const created = await (await createExercise("Bodyweight Squat")).json();

        const response = await SELF.fetch(`http://worker/api/exercises/${created.id}`, {
            method: "PATCH",
            headers: { "content-type": "application/json" },
            body: JSON.stringify({ name: "Bodyweight Squat" }),
        });

        expect(response.status).toBe(200);
    });
});

describe("DELETE /api/exercises/:id", () => {
    it("deletes an existing exercise", async () => {
        const created = await (await createExercise("Bodyweight Squat")).json();

        const response = await SELF.fetch(`http://worker/api/exercises/${created.id}`, {
            method: "DELETE",
        });

        expect(response.status).toBe(204);

        const list = await (await SELF.fetch("http://worker/api/exercises")).json();
        expect(list).toHaveLength(0);
    });

    it("returns 404 for an unknown id", async () => {
        const response = await SELF.fetch("http://worker/api/exercises/EXR-ZZZZZZ", {
            method: "DELETE",
        });

        expect(response.status).toBe(404);
    });

    it("returns 409 and makes no change when the exercise is referenced by a program exercise", async () => {
        const exercise = await (await createExercise("Bodyweight Squat")).json();
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();
        const phase = await (await createPhase("Warm Up")).json();
        const sessionPhase = await (
            await createSessionPhase(program.id, session.id, phase.id)
        ).json();
        await createProgramExercise(program.id, session.id, sessionPhase.id, {
            exerciseId: exercise.id,
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
        });

        const response = await SELF.fetch(`http://worker/api/exercises/${exercise.id}`, {
            method: "DELETE",
        });

        expect(response.status).toBe(409);
        const list = await (await SELF.fetch("http://worker/api/exercises")).json();
        expect(list).toHaveLength(1);
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

describe("GET /api/programs/:programId/sessions/:sessionId/phases/:sessionPhaseId/exercises", () => {
    it("returns an empty array when the session phase has no program exercises", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();
        const phase = await (await createPhase("Warm Up")).json();
        const sessionPhase = await (
            await createSessionPhase(program.id, session.id, phase.id)
        ).json();

        const response = await SELF.fetch(
            programExercisesUrl(program.id, session.id, sessionPhase.id),
        );

        expect(response.status).toBe(200);
        expect(await response.json()).toEqual([]);
    });

    it("returns the session phase's program exercises ordered by creation", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();
        const phase = await (await createPhase("Warm Up")).json();
        const sessionPhase = await (
            await createSessionPhase(program.id, session.id, phase.id)
        ).json();
        const exercise = await (await createExercise("Bodyweight Squat")).json();
        await createProgramExercise(program.id, session.id, sessionPhase.id, {
            exerciseId: exercise.id,
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
        });

        const response = await SELF.fetch(
            programExercisesUrl(program.id, session.id, sessionPhase.id),
        );
        const programExercises = await response.json();

        expect(programExercises).toHaveLength(1);
        expect(programExercises[0].exerciseId).toBe(exercise.id);
    });

    it("returns 404 for a sessionPhaseId that does not exist under that session", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();

        const response = await SELF.fetch(
            programExercisesUrl(program.id, session.id, "SPH-ZZZZZZ"),
        );

        expect(response.status).toBe(404);
    });

    it("returns 404 for an unknown sessionId", async () => {
        const program = await (await createProgram("Workout A")).json();

        const response = await SELF.fetch(
            programExercisesUrl(program.id, "SNN-ZZZZZZ", "SPH-ZZZZZZ"),
        );

        expect(response.status).toBe(404);
    });
});

describe("POST /api/programs/:programId/sessions/:sessionId/phases/:sessionPhaseId/exercises", () => {
    it("creates a Reps program exercise defaulting weight to 0 and side to Both", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();
        const phase = await (await createPhase("Warm Up")).json();
        const sessionPhase = await (
            await createSessionPhase(program.id, session.id, phase.id)
        ).json();
        const exercise = await (await createExercise("Bodyweight Squat")).json();

        const response = await createProgramExercise(program.id, session.id, sessionPhase.id, {
            exerciseId: exercise.id,
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
        });

        expect(response.status).toBe(201);
        const created = await response.json();
        expect(created.weight).toBe(0);
        expect(created.side).toBe("Both");
        expect(created).not.toHaveProperty("durationSeconds");
    });

    it("creates a Timed program exercise with no reps property", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();
        const phase = await (await createPhase("Warm Up")).json();
        const sessionPhase = await (
            await createSessionPhase(program.id, session.id, phase.id)
        ).json();
        const exercise = await (await createExercise("Bodyweight Squat")).json();

        const response = await createProgramExercise(program.id, session.id, sessionPhase.id, {
            exerciseId: exercise.id,
            type: "Timed",
            durationSeconds: 30,
            sets: 3,
            restSeconds: 60,
        });

        expect(response.status).toBe(201);
        const created = await response.json();
        expect(created).not.toHaveProperty("reps");
    });

    it("returns 404 for a sessionPhaseId that does not exist under that session", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();
        const exercise = await (await createExercise("Bodyweight Squat")).json();

        const response = await createProgramExercise(program.id, session.id, "SPH-ZZZZZZ", {
            exerciseId: exercise.id,
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
        });

        expect(response.status).toBe(404);
    });

    it("returns 400 for a missing exerciseId", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();
        const phase = await (await createPhase("Warm Up")).json();
        const sessionPhase = await (
            await createSessionPhase(program.id, session.id, phase.id)
        ).json();

        const response = await createProgramExercise(program.id, session.id, sessionPhase.id, {
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
        });

        expect(response.status).toBe(400);
    });

    it("returns 400 for an exerciseId that matches no exercise", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();
        const phase = await (await createPhase("Warm Up")).json();
        const sessionPhase = await (
            await createSessionPhase(program.id, session.id, phase.id)
        ).json();

        const response = await createProgramExercise(program.id, session.id, sessionPhase.id, {
            exerciseId: "EXR-ZZZZZZ",
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
        });

        expect(response.status).toBe(400);
    });

    it("returns 400 for a missing or invalid type", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();
        const phase = await (await createPhase("Warm Up")).json();
        const sessionPhase = await (
            await createSessionPhase(program.id, session.id, phase.id)
        ).json();
        const exercise = await (await createExercise("Bodyweight Squat")).json();

        const response = await createProgramExercise(program.id, session.id, sessionPhase.id, {
            exerciseId: exercise.id,
            type: "Bogus",
            reps: 10,
            sets: 3,
            restSeconds: 60,
        });

        expect(response.status).toBe(400);
    });

    it("returns 400 for a Reps body that also carries durationSeconds", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();
        const phase = await (await createPhase("Warm Up")).json();
        const sessionPhase = await (
            await createSessionPhase(program.id, session.id, phase.id)
        ).json();
        const exercise = await (await createExercise("Bodyweight Squat")).json();

        const response = await createProgramExercise(program.id, session.id, sessionPhase.id, {
            exerciseId: exercise.id,
            type: "Reps",
            reps: 10,
            durationSeconds: 30,
            sets: 3,
            restSeconds: 60,
        });

        expect(response.status).toBe(400);
    });

    it("returns 400 for a negative weight", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();
        const phase = await (await createPhase("Warm Up")).json();
        const sessionPhase = await (
            await createSessionPhase(program.id, session.id, phase.id)
        ).json();
        const exercise = await (await createExercise("Bodyweight Squat")).json();

        const response = await createProgramExercise(program.id, session.id, sessionPhase.id, {
            exerciseId: exercise.id,
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
            weight: -1,
        });

        expect(response.status).toBe(400);
    });

    it("returns 400 for a side that is not Both/Left/Right", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();
        const phase = await (await createPhase("Warm Up")).json();
        const sessionPhase = await (
            await createSessionPhase(program.id, session.id, phase.id)
        ).json();
        const exercise = await (await createExercise("Bodyweight Squat")).json();

        const response = await createProgramExercise(program.id, session.id, sessionPhase.id, {
            exerciseId: exercise.id,
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
            side: "Up",
        });

        expect(response.status).toBe(400);
    });

    it("creates a second independent row when the same exerciseId is added twice", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();
        const phase = await (await createPhase("Warm Up")).json();
        const sessionPhase = await (
            await createSessionPhase(program.id, session.id, phase.id)
        ).json();
        const exercise = await (await createExercise("Bodyweight Squat")).json();
        await createProgramExercise(program.id, session.id, sessionPhase.id, {
            exerciseId: exercise.id,
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
        });

        const response = await createProgramExercise(program.id, session.id, sessionPhase.id, {
            exerciseId: exercise.id,
            type: "Reps",
            reps: 8,
            sets: 3,
            restSeconds: 60,
        });

        expect(response.status).toBe(201);
        const list = await (
            await SELF.fetch(programExercisesUrl(program.id, session.id, sessionPhase.id))
        ).json();
        expect(list).toHaveLength(2);
    });
});

describe("PATCH /api/programs/:programId/sessions/:sessionId/phases/:sessionPhaseId/exercises/:id", () => {
    async function setUp() {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();
        const phase = await (await createPhase("Warm Up")).json();
        const sessionPhase = await (
            await createSessionPhase(program.id, session.id, phase.id)
        ).json();
        const exercise = await (await createExercise("Bodyweight Squat")).json();
        const programExercise = await (
            await createProgramExercise(program.id, session.id, sessionPhase.id, {
                exerciseId: exercise.id,
                type: "Reps",
                reps: 10,
                sets: 3,
                restSeconds: 60,
            })
        ).json();
        return { program, session, sessionPhase, exercise, programExercise };
    }

    it("updates the provided fields", async () => {
        const { program, session, sessionPhase, programExercise } = await setUp();

        const response = await SELF.fetch(
            programExercisesUrl(program.id, session.id, sessionPhase.id, programExercise.id),
            {
                method: "PATCH",
                headers: { "content-type": "application/json" },
                body: JSON.stringify({ reps: 12, weight: 45 }),
            },
        );

        expect(response.status).toBe(200);
        const updated = await response.json();
        expect(updated.reps).toBe(12);
        expect(updated.weight).toBe(45);
    });

    it("returns 404 for an id that does not exist under that session phase", async () => {
        const { program, session, sessionPhase } = await setUp();

        const response = await SELF.fetch(
            programExercisesUrl(program.id, session.id, sessionPhase.id, "PGX-ZZZZZZ"),
            {
                method: "PATCH",
                headers: { "content-type": "application/json" },
                body: JSON.stringify({ weight: 45 }),
            },
        );

        expect(response.status).toBe(404);
    });

    it("returns 400 instead of throwing when the body is a JSON primitive", async () => {
        const { program, session, sessionPhase, programExercise } = await setUp();

        const response = await SELF.fetch(
            programExercisesUrl(program.id, session.id, sessionPhase.id, programExercise.id),
            {
                method: "PATCH",
                headers: { "content-type": "application/json" },
                body: JSON.stringify(123),
            },
        );

        expect(response.status).toBe(400);
    });

    it("returns 400 when the body carries exerciseId", async () => {
        const { program, session, sessionPhase, programExercise, exercise } = await setUp();

        const response = await SELF.fetch(
            programExercisesUrl(program.id, session.id, sessionPhase.id, programExercise.id),
            {
                method: "PATCH",
                headers: { "content-type": "application/json" },
                body: JSON.stringify({ exerciseId: exercise.id }),
            },
        );

        expect(response.status).toBe(400);
    });

    it("returns 400 when the body carries type", async () => {
        const { program, session, sessionPhase, programExercise } = await setUp();

        const response = await SELF.fetch(
            programExercisesUrl(program.id, session.id, sessionPhase.id, programExercise.id),
            {
                method: "PATCH",
                headers: { "content-type": "application/json" },
                body: JSON.stringify({ type: "Timed" }),
            },
        );

        expect(response.status).toBe(400);
    });

    it("returns 400 when a required numeric field is set to zero", async () => {
        const { program, session, sessionPhase, programExercise } = await setUp();

        const response = await SELF.fetch(
            programExercisesUrl(program.id, session.id, sessionPhase.id, programExercise.id),
            {
                method: "PATCH",
                headers: { "content-type": "application/json" },
                body: JSON.stringify({ reps: 0 }),
            },
        );

        expect(response.status).toBe(400);
    });

    it("returns 400 when weight is set negative", async () => {
        const { program, session, sessionPhase, programExercise } = await setUp();

        const response = await SELF.fetch(
            programExercisesUrl(program.id, session.id, sessionPhase.id, programExercise.id),
            {
                method: "PATCH",
                headers: { "content-type": "application/json" },
                body: JSON.stringify({ weight: -1 }),
            },
        );

        expect(response.status).toBe(400);
    });

    it("returns 400 when side is set to an invalid value", async () => {
        const { program, session, sessionPhase, programExercise } = await setUp();

        const response = await SELF.fetch(
            programExercisesUrl(program.id, session.id, sessionPhase.id, programExercise.id),
            {
                method: "PATCH",
                headers: { "content-type": "application/json" },
                body: JSON.stringify({ side: "Up" }),
            },
        );

        expect(response.status).toBe(400);
    });

    it("returns 400 when durationSeconds is set on a Reps-type row", async () => {
        const { program, session, sessionPhase, programExercise } = await setUp();

        const response = await SELF.fetch(
            programExercisesUrl(program.id, session.id, sessionPhase.id, programExercise.id),
            {
                method: "PATCH",
                headers: { "content-type": "application/json" },
                body: JSON.stringify({ durationSeconds: 30 }),
            },
        );

        expect(response.status).toBe(400);
    });

    it("returns 400 when reps is set on a Timed-type row", async () => {
        const { program, session, sessionPhase, exercise } = await setUp();
        const timedProgramExercise = await (
            await createProgramExercise(program.id, session.id, sessionPhase.id, {
                exerciseId: exercise.id,
                type: "Timed",
                durationSeconds: 30,
                sets: 3,
                restSeconds: 60,
            })
        ).json();

        const response = await SELF.fetch(
            programExercisesUrl(
                program.id,
                session.id,
                sessionPhase.id,
                timedProgramExercise.id,
            ),
            {
                method: "PATCH",
                headers: { "content-type": "application/json" },
                body: JSON.stringify({ reps: 10 }),
            },
        );

        expect(response.status).toBe(400);
    });
});

describe("DELETE /api/programs/:programId/sessions/:sessionId/phases/:sessionPhaseId/exercises/:id", () => {
    it("deletes an existing program exercise", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();
        const phase = await (await createPhase("Warm Up")).json();
        const sessionPhase = await (
            await createSessionPhase(program.id, session.id, phase.id)
        ).json();
        const exercise = await (await createExercise("Bodyweight Squat")).json();
        const programExercise = await (
            await createProgramExercise(program.id, session.id, sessionPhase.id, {
                exerciseId: exercise.id,
                type: "Reps",
                reps: 10,
                sets: 3,
                restSeconds: 60,
            })
        ).json();

        const response = await SELF.fetch(
            programExercisesUrl(program.id, session.id, sessionPhase.id, programExercise.id),
            { method: "DELETE" },
        );

        expect(response.status).toBe(204);
        const list = await (
            await SELF.fetch(programExercisesUrl(program.id, session.id, sessionPhase.id))
        ).json();
        expect(list).toHaveLength(0);
    });

    it("returns 404 for an unknown id", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();
        const phase = await (await createPhase("Warm Up")).json();
        const sessionPhase = await (
            await createSessionPhase(program.id, session.id, phase.id)
        ).json();

        const response = await SELF.fetch(
            programExercisesUrl(program.id, session.id, sessionPhase.id, "PGX-ZZZZZZ"),
            { method: "DELETE" },
        );

        expect(response.status).toBe(404);
    });
});

describe("DELETE /api/programs/:programId/sessions/:sessionId/phases/:id cascades to program exercises", () => {
    it("removes a deleted session phase's program exercises", async () => {
        const program = await (await createProgram("Workout A")).json();
        const session = await (await createSession(program.id, "Monday Lower Body")).json();
        const phase = await (await createPhase("Warm Up")).json();
        const sessionPhase = await (
            await createSessionPhase(program.id, session.id, phase.id)
        ).json();
        const exercise = await (await createExercise("Bodyweight Squat")).json();
        await createProgramExercise(program.id, session.id, sessionPhase.id, {
            exerciseId: exercise.id,
            type: "Reps",
            reps: 10,
            sets: 3,
            restSeconds: 60,
        });

        const deleteResponse = await SELF.fetch(
            `http://worker/api/programs/${program.id}/sessions/${session.id}/phases/${sessionPhase.id}`,
            { method: "DELETE" },
        );
        expect(deleteResponse.status).toBe(204);

        const listResponse = await SELF.fetch(
            programExercisesUrl(program.id, session.id, sessionPhase.id),
        );
        // The session phase no longer exists, so its (now cascade-deleted) program
        // exercises are unreachable via a 404 rather than an empty list -- this also
        // proves the rows were actually removed by D1's FK cascade, not just orphaned.
        expect(listResponse.status).toBe(404);
    });
});
