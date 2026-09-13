import path from "node:path";
import { fileURLToPath } from "node:url";
import { defineWorkersConfig, readD1Migrations } from "@cloudflare/vitest-pool-workers/config";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

export default defineWorkersConfig(async () => {
    // IdentityApi has no migrations of its own -- the logins/users/roles schema it
    // reads is owned by AdminApi's migration history (see CLAUDE.md and
    // specs/identity-api/spec.md's "Identity schema migration rides AdminApi's
    // existing migration history" requirement).
    const migrationsPath = path.join(__dirname, "..", "Trainfree.AdminApi", "migrations");
    const migrations = await readD1Migrations(migrationsPath);

    return {
        test: {
            // No test files exist yet this task group; later slices add them.
            passWithNoTests: true,
            setupFiles: ["./test/apply-migrations.js"],
            poolOptions: {
                workers: {
                    wrangler: { configPath: "./wrangler.jsonc" },
                    miniflare: {
                        bindings: {
                            TEST_MIGRATIONS: migrations,
                        },
                    },
                },
            },
        },
    };
});
