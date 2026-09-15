// Never invoked -- see wrangler.jsonc's comment on `main`. Present only to satisfy
// wrangler's config schema for a config this repo never runs with `dev`/`deploy`.
export default {
    async fetch() {
        return new Response("not used", { status: 501 });
    },
};
