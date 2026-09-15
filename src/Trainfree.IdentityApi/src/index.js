import { handleInternalIdentity } from "./identity/internal-identity.js";
import { jsonError } from "./shared/http.js";
import { versionStamp } from "./version.js";

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

    // no-store, not just no-cache: this response is the one thing deploy.yaml's CI
    // check reads to confirm the deployed stamp, so it must never be answered from
    // any cache.
    const response = jsonResponse(versionStamp(env));
    response.headers.set("cache-control", "no-store");
    return response;
}

export default {
    async fetch(request, env) {
        const url = new URL(request.url);

        if (url.pathname === "/internal/identity" && request.method === "GET") {
            return handleInternalIdentity(request, env);
        }

        if (url.pathname === "/api/version") {
            return handleVersion(request, env);
        }

        return jsonError("not found", 404);
    },
};
