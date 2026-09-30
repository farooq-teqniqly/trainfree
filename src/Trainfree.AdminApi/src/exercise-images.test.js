import { env, SELF } from "cloudflare:test";
import { describe, expect, it, vi } from "vitest";
import worker from "./index.js";
import { MAX_IMAGE_BYTES } from "./image-validation.js";

const PNG_SIGNATURE = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];
const JPEG_SIGNATURE = [0xff, 0xd8, 0xff];

function imageBytes(signature, length = 64, fill = 0) {
    const bytes = new Uint8Array(length).fill(fill);
    bytes.set(signature);
    return bytes;
}

function pngBytes(length, fill) {
    return imageBytes(PNG_SIGNATURE, length, fill);
}

function imageUrlFor(id) {
    return `http://worker/api/exercises/${id}/image`;
}

async function postJson(url, body) {
    const response = await SELF.fetch(url, {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify(body),
    });
    return response.json();
}

function createExercise(name) {
    return postJson("http://worker/api/exercises", { name });
}

function putImage(id, body, contentType = "image/png") {
    return SELF.fetch(imageUrlFor(id), {
        method: "PUT",
        headers: { "content-type": contentType },
        body,
    });
}

async function storedKey(id) {
    const row = await env.DB.prepare(
        "SELECT image_key as imageKey FROM exercises WHERE exercise_id = ?",
    )
        .bind(id)
        .first();
    return row?.imageKey ?? null;
}

async function r2ObjectCount() {
    const listed = await env.IMAGES.list();
    return listed.objects.length;
}

async function createUsedExercise(name) {
    const exercise = await createExercise(name);
    const program = await postJson("http://worker/api/programs", { name: "Workout A" });
    const session = await postJson(`http://worker/api/programs/${program.id}/sessions`, {
        name: "Monday Lower Body",
    });
    const phase = await postJson("http://worker/api/phases", { name: "Warm Up" });
    const sessionPhase = await postJson(
        `http://worker/api/programs/${program.id}/sessions/${session.id}/phases`,
        { phaseId: phase.id },
    );
    await postJson(
        `http://worker/api/programs/${program.id}/sessions/${session.id}/phases/${sessionPhase.id}/exercises`,
        { exerciseId: exercise.id, type: "Reps", reps: 10, sets: 3, restSeconds: 60 },
    );
    return exercise;
}

describe("PUT /api/exercises/:id/image", () => {
    it("stores the first upload and returns the exercise with a non-null imageUrl", async () => {
        const exercise = await createExercise("Bodyweight Squat");

        const response = await putImage(exercise.id, pngBytes(64));
        const body = await response.json();

        expect(response.status).toBe(200);
        expect(body.id).toBe(exercise.id);
        expect(body.imageUrl).toBeTypeOf("string");
        expect(await r2ObjectCount()).toBe(1);
        const object = await env.IMAGES.head(await storedKey(exercise.id));
        expect(object.httpMetadata.contentType).toBe("image/png");
    });

    it("changes imageUrl and deletes the old object on replace", async () => {
        const exercise = await createExercise("Bodyweight Squat");
        const first = await (await putImage(exercise.id, pngBytes(64, 1))).json();
        const oldKey = await storedKey(exercise.id);

        const response = await putImage(exercise.id, pngBytes(64, 2));
        const second = await response.json();

        expect(response.status).toBe(200);
        expect(second.imageUrl).not.toBe(first.imageUrl);
        expect(await env.IMAGES.head(oldKey)).toBeNull();
        expect(await r2ObjectCount()).toBe(1);
    });

    it("returns 404 and stores nothing for an unknown exercise", async () => {
        const response = await putImage("EXR-ZZZZZZ", pngBytes(64));

        expect(response.status).toBe(404);
        expect(await r2ObjectCount()).toBe(0);
    });

    it("accepts and replaces an image on an exercise used by a program exercise, and deletes it", async () => {
        const exercise = await createUsedExercise("Bodyweight Squat");

        const first = await putImage(exercise.id, pngBytes(64, 1));
        const second = await putImage(exercise.id, pngBytes(64, 2));
        const deleted = await SELF.fetch(imageUrlFor(exercise.id), { method: "DELETE" });

        expect(first.status).toBe(200);
        expect(second.status).toBe(200);
        expect(deleted.status).toBe(204);
        const used = await env.DB.prepare(
            "SELECT COUNT(*) as n FROM program_exercises WHERE exercise_id = ?",
        )
            .bind(exercise.id)
            .first();
        expect(used.n).toBe(1);
    });

    it("accepts a JPEG", async () => {
        const exercise = await createExercise("Bodyweight Squat");

        const response = await putImage(exercise.id, imageBytes(JPEG_SIGNATURE), "image/jpeg");

        expect(response.status).toBe(200);
    });

    it("accepts a body of exactly the size limit", async () => {
        const exercise = await createExercise("Bodyweight Squat");

        const response = await putImage(exercise.id, pngBytes(MAX_IMAGE_BYTES));

        expect(response.status).toBe(200);
    });

    it.each([
        ["an oversized body", () => pngBytes(MAX_IMAGE_BYTES + 1), "image/png", 413],
        ["an unsupported declared type", () => pngBytes(64), "image/webp", 415],
        ["a signature mismatch", () => pngBytes(64), "image/jpeg", 415],
        ["an empty body", () => new Uint8Array(0), "image/png", 400],
    ])("rejects %s with a JSON error and changes nothing", async (_name, makeBody, type, status) => {
        const exercise = await createExercise("Bodyweight Squat");

        const response = await putImage(exercise.id, makeBody(), type);

        expect(response.status).toBe(status);
        expect((await response.json()).error).toBeTypeOf("string");
        expect(await storedKey(exercise.id)).toBeNull();
        expect(await r2ObjectCount()).toBe(0);
    });

    it("rejects a large declared Content-Length with 413 before reading the body", async () => {
        const exercise = await createExercise("Bodyweight Squat");
        const stream = new ReadableStream({
            start(controller) {
                controller.enqueue(pngBytes(64));
                controller.close();
            },
        });
        const request = new Request(imageUrlFor(exercise.id), {
            method: "PUT",
            headers: {
                "content-type": "image/png",
                "content-length": String(MAX_IMAGE_BYTES + 1),
            },
            body: stream,
            duplex: "half",
        });

        const response = await worker.fetch(request, fakeEnvFor(identityOk("Administrator")));

        expect(response.status).toBe(413);
        expect((await response.json()).error).toBeTypeOf("string");
        expect(request.bodyUsed).toBe(false);
        expect(await storedKey(exercise.id)).toBeNull();
        expect(await r2ObjectCount()).toBe(0);
    });

    it("rejects an oversize streamed body without Content-Length with 413 and stops reading", async () => {
        const exercise = await createExercise("Bodyweight Squat");
        const chunk = pngBytes(MAX_IMAGE_BYTES / 4);
        let pulls = 0;
        const stream = new ReadableStream({
            pull(controller) {
                pulls += 1;
                if (pulls > 50) {
                    controller.close();
                    return;
                }
                controller.enqueue(chunk);
            },
        });
        const request = new Request(imageUrlFor(exercise.id), {
            method: "PUT",
            headers: { "content-type": "image/png" },
            body: stream,
            duplex: "half",
        });
        expect(request.headers.get("content-length")).toBeNull();

        const response = await worker.fetch(request, fakeEnvFor(identityOk("Administrator")));

        expect(response.status).toBe(413);
        expect((await response.json()).error).toBeTypeOf("string");
        expect(pulls).toBeLessThan(10);
        expect(await storedKey(exercise.id)).toBeNull();
        expect(await r2ObjectCount()).toBe(0);
    });

    it("accepts a streamed body of exactly the size limit without Content-Length", async () => {
        const exercise = await createExercise("Bodyweight Squat");
        const whole = pngBytes(MAX_IMAGE_BYTES);
        const half = MAX_IMAGE_BYTES / 2;
        const stream = new ReadableStream({
            start(controller) {
                controller.enqueue(whole.slice(0, half));
                controller.enqueue(whole.slice(half));
                controller.close();
            },
        });
        const request = new Request(imageUrlFor(exercise.id), {
            method: "PUT",
            headers: { "content-type": "image/png" },
            body: stream,
            duplex: "half",
        });

        const response = await worker.fetch(request, fakeEnvFor(identityOk("Administrator")));

        expect(response.status).toBe(200);
        const object = await env.IMAGES.head(await storedKey(exercise.id));
        expect(object.size).toBe(MAX_IMAGE_BYTES);
    });

    it("keeps the existing image when a replacement is rejected", async () => {
        const exercise = await createExercise("Bodyweight Squat");
        await putImage(exercise.id, pngBytes(64));
        const key = await storedKey(exercise.id);

        const response = await putImage(exercise.id, pngBytes(64), "image/webp");

        expect(response.status).toBe(415);
        expect(await storedKey(exercise.id)).toBe(key);
        expect(await env.IMAGES.head(key)).not.toBeNull();
    });
});

describe("GET /api/exercises/:id/image", () => {
    it("returns the stored bytes, content type, and an immutable cache header", async () => {
        const exercise = await createExercise("Bodyweight Squat");
        const bytes = pngBytes(64, 7);
        await putImage(exercise.id, bytes);

        const response = await SELF.fetch(imageUrlFor(exercise.id));

        expect(response.status).toBe(200);
        expect(response.headers.get("content-type")).toBe("image/png");
        expect(response.headers.get("cache-control")).toContain("immutable");
        expect(new Uint8Array(await response.arrayBuffer())).toEqual(bytes);
    });

    it("sends X-Content-Type-Options nosniff", async () => {
        const exercise = await createExercise("Bodyweight Squat");
        await putImage(exercise.id, pngBytes(64));

        const response = await SELF.fetch(imageUrlFor(exercise.id));
        await response.arrayBuffer();

        expect(response.headers.get("x-content-type-options")).toBe("nosniff");
    });

    it("falls back to application/octet-stream when the stored object has no content type", async () => {
        const exercise = await createExercise("Bodyweight Squat");
        await putImage(exercise.id, pngBytes(64));
        await env.IMAGES.put(await storedKey(exercise.id), pngBytes(64));

        const response = await SELF.fetch(imageUrlFor(exercise.id));
        await response.arrayBuffer();

        expect(response.status).toBe(200);
        expect(response.headers.get("content-type")).toBe("application/octet-stream");
    });

    it("returns 404 for an exercise with no image", async () => {
        const exercise = await createExercise("Bodyweight Squat");

        const response = await SELF.fetch(imageUrlFor(exercise.id));

        expect(response.status).toBe(404);
    });

    it("returns 404 for an unknown exercise", async () => {
        const response = await SELF.fetch(imageUrlFor("EXR-ZZZZZZ"));

        expect(response.status).toBe(404);
    });

    it("serves the image at the imageUrl the API returns", async () => {
        const exercise = await createExercise("Bodyweight Squat");
        const updated = await (await putImage(exercise.id, pngBytes(64))).json();

        const response = await SELF.fetch(`http://worker${updated.imageUrl}`);
        await response.arrayBuffer();

        expect(response.status).toBe(200);
    });
});

describe("DELETE /api/exercises/:id/image", () => {
    it("returns 204, clears the key, and removes the R2 object", async () => {
        const exercise = await createExercise("Bodyweight Squat");
        await putImage(exercise.id, pngBytes(64));

        const response = await SELF.fetch(imageUrlFor(exercise.id), { method: "DELETE" });

        expect(response.status).toBe(204);
        expect(await r2ObjectCount()).toBe(0);
        const list = await (await SELF.fetch("http://worker/api/exercises")).json();
        expect(list[0].imageUrl).toBeNull();
    });

    it("returns 404 for an exercise with no image", async () => {
        const exercise = await createExercise("Bodyweight Squat");

        const response = await SELF.fetch(imageUrlFor(exercise.id), { method: "DELETE" });

        expect(response.status).toBe(404);
    });

    it("returns 404 for an unknown exercise", async () => {
        const response = await SELF.fetch(imageUrlFor("EXR-ZZZZZZ"), { method: "DELETE" });

        expect(response.status).toBe(404);
    });
});

describe("unsupported methods on the image route", () => {
    it("returns 405 for POST", async () => {
        const exercise = await createExercise("Bodyweight Squat");

        const response = await SELF.fetch(imageUrlFor(exercise.id), { method: "POST" });

        expect(response.status).toBe(405);
    });
});

describe("exercise responses expose imageUrl", () => {
    it("is null in create, list, and rename responses for an exercise without an image", async () => {
        const created = await createExercise("Bodyweight Squat");

        const renamed = await (
            await SELF.fetch(`http://worker/api/exercises/${created.id}`, {
                method: "PATCH",
                headers: { "content-type": "application/json" },
                body: JSON.stringify({ name: "Air Squat" }),
            })
        ).json();
        const list = await (await SELF.fetch("http://worker/api/exercises")).json();

        expect(created.imageUrl).toBeNull();
        expect(renamed.imageUrl).toBeNull();
        expect(list[0].imageUrl).toBeNull();
    });

    it("is a relative path in list and rename responses, and never exposes the key", async () => {
        const created = await createExercise("Bodyweight Squat");
        await putImage(created.id, pngBytes(64));
        const key = await storedKey(created.id);

        const renamed = await (
            await SELF.fetch(`http://worker/api/exercises/${created.id}`, {
                method: "PATCH",
                headers: { "content-type": "application/json" },
                body: JSON.stringify({ name: "Air Squat" }),
            })
        ).json();
        const list = await (await SELF.fetch("http://worker/api/exercises")).json();

        expect(list[0].imageUrl).toMatch(/^\/api\/exercises\/EXR-[A-Z0-9]{6}\/image/);
        expect(renamed.imageUrl).toBe(list[0].imageUrl);
        for (const exercise of [renamed, list[0]]) {
            expect(Object.keys(exercise)).not.toContain("imageKey");
            expect(JSON.stringify(exercise)).not.toContain(key);
            expect(JSON.stringify(exercise)).not.toContain("trainfree-exercise-images");
        }
    });
});

describe("DELETE /api/exercises/:id with an image", () => {
    it("removes the R2 object for an unused exercise", async () => {
        const exercise = await createExercise("Bodyweight Squat");
        await putImage(exercise.id, pngBytes(64));

        const response = await SELF.fetch(`http://worker/api/exercises/${exercise.id}`, {
            method: "DELETE",
        });

        expect(response.status).toBe(204);
        expect(await r2ObjectCount()).toBe(0);
    });

    it("leaves the R2 object intact on the 409 in-use path", async () => {
        const exercise = await createUsedExercise("Bodyweight Squat");
        await putImage(exercise.id, pngBytes(64));
        const key = await storedKey(exercise.id);

        const response = await SELF.fetch(`http://worker/api/exercises/${exercise.id}`, {
            method: "DELETE",
        });

        expect(response.status).toBe(409);
        expect(await env.IMAGES.head(key)).not.toBeNull();
    });
});

function fakeEnvFor(identityResponse) {
    return {
        DB: env.DB,
        IMAGES: env.IMAGES,
        ADMIN_INTERNAL_KEY: "the-internal-key",
        IDENTITY: { fetch: vi.fn().mockResolvedValue(identityResponse) },
    };
}

function identityOk(role) {
    return new Response(
        JSON.stringify({ email: "someone@example.com", userId: "USR-SOMEONE", role }),
        { status: 200, headers: { "content-type": "application/json" } },
    );
}

describe("image access by role", () => {
    it("rejects PUT from a User with 403 and stores nothing", async () => {
        const exercise = await createExercise("Bodyweight Squat");
        const request = new Request(imageUrlFor(exercise.id), {
            method: "PUT",
            headers: { "content-type": "image/png" },
            body: pngBytes(64),
        });

        const response = await worker.fetch(request, fakeEnvFor(identityOk("User")));

        expect(response.status).toBe(403);
        expect(await storedKey(exercise.id)).toBeNull();
        expect(await r2ObjectCount()).toBe(0);
    });

    it("rejects DELETE from a User with 403 and changes nothing", async () => {
        const exercise = await createExercise("Bodyweight Squat");
        await putImage(exercise.id, pngBytes(64));
        const request = new Request(imageUrlFor(exercise.id), { method: "DELETE" });

        const response = await worker.fetch(request, fakeEnvFor(identityOk("User")));

        expect(response.status).toBe(403);
        expect(await r2ObjectCount()).toBe(1);
        expect(await storedKey(exercise.id)).not.toBeNull();
    });

    it.each(["User", "Administrator"])("lets %s read an image with 200", async (role) => {
        const exercise = await createExercise("Bodyweight Squat");
        await putImage(exercise.id, pngBytes(64));

        const response = await worker.fetch(
            new Request(imageUrlFor(exercise.id)),
            fakeEnvFor(identityOk(role)),
        );
        await response.arrayBuffer();

        expect(response.status).toBe(200);
    });

    it.each([401, 403])("relays IdentityApi's %i on image read without reading R2", async (status) => {
        const exercise = await createExercise("Bodyweight Squat");
        await putImage(exercise.id, pngBytes(64));
        const fakeEnv = fakeEnvFor(new Response(null, { status }));
        const fakeImages = { get: vi.fn(), head: vi.fn() };
        fakeEnv.IMAGES = fakeImages;

        const response = await worker.fetch(new Request(imageUrlFor(exercise.id)), fakeEnv);

        expect(response.status).toBe(status);
        expect(fakeImages.get).not.toHaveBeenCalled();
    });

    it("still rejects a User on the other exercise routes", async () => {
        const response = await worker.fetch(
            new Request("http://worker/api/exercises"),
            fakeEnvFor(identityOk("User")),
        );

        expect(response.status).toBe(403);
    });
});
