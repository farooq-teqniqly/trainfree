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
                    },
                },
            },
        },
    };
});
