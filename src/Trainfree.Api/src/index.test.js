import { SELF } from "cloudflare:test";
import { describe, expect, it } from "vitest";

async function createProgram(name) {
    return SELF.fetch("http://worker/api/programs", {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({ name }),
    });
}

describe("CORS", () => {
    it("responds to an OPTIONS preflight with allow headers and no body", async () => {
        const response = await SELF.fetch("http://worker/api/programs", {
            method: "OPTIONS",
            headers: {
                "Access-Control-Request-Method": "POST",
                Origin: "http://localhost:5280",
            },
        });

        expect(response.status).toBe(204);
        expect(response.headers.get("access-control-allow-origin")).toBe("*");
        expect(response.headers.get("access-control-allow-methods")).toContain("POST");
        expect(await response.text()).toBe("");
    });

    it("includes Access-Control-Allow-Origin on normal responses", async () => {
        const response = await SELF.fetch("http://worker/api/programs");

        expect(response.headers.get("access-control-allow-origin")).toBe("*");
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
