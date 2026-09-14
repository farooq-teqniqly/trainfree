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
                            // Stand in for the real per-caller secrets/vars task 9.3's
                            // manual Access setup provides in production; the audience
                            // and issuer values here match the constants the JWT/
                            // provider unit tests sign their test JWTs against.
                            ACCESS_TEAM_DOMAIN: "trainfree",
                            ADMIN_AUDIENCE: "trainfree-admin-audience",
                            WORKOUT_AUDIENCE: "trainfree-workout-audience",
                            ADMIN_INTERNAL_KEY: "test-admin-internal-key",
                            WORKOUT_INTERNAL_KEY: "test-workout-internal-key",
                        },
                    },
                },
            },
        },
    };
});
