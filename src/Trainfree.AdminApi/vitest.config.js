import path from "node:path";
import { fileURLToPath } from "node:url";
import { defineWorkersConfig, readD1Migrations } from "@cloudflare/vitest-pool-workers/config";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

export default defineWorkersConfig(async () => {
    const migrationsPath = path.join(__dirname, "migrations");
    const migrations = await readD1Migrations(migrationsPath);

    return {
        test: {
            setupFiles: ["./test/apply-migrations.js"],
            poolOptions: {
                workers: {
                    wrangler: { configPath: "./wrangler.jsonc" },
                    miniflare: {
                        bindings: {
                            TEST_MIGRATIONS: migrations,
                            // Stand in for the values deploy.yaml stamps in with
                            // `wrangler deploy --var`.
                            APP_VERSION: "v9.9.9",
                            APP_COMMIT: "abc1234",
                        },
                        // wrangler.jsonc's IDENTITY service binding names a real
                        // Worker ("trainfree-identity-api") that this test pool never
                        // runs -- Miniflare fails to start without something bound to
                        // that name. LOCAL_DEV_BYPASS is also set by default in
                        // wrangler.jsonc, so identity.js never actually calls this
                        // fetcher; it only needs to exist for Miniflare's binding
                        // resolution. Tests that exercise the real (non-bypass) path
                        // override env.IDENTITY.fetch directly instead.
                        serviceBindings: {
                            IDENTITY: () =>
                                new Response(null, { status: 503 }),
                        },
                    },
                },
            },
        },
    };
});
