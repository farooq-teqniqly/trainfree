import { handleInternalIdentity } from "./identity/internal-identity.js";

export default {
    async fetch(request, env) {
        const url = new URL(request.url);

        if (url.pathname === "/internal/identity" && request.method === "GET") {
            return handleInternalIdentity(request, env);
        }

        return new Response("Not Found", { status: 404 });
    },
};
