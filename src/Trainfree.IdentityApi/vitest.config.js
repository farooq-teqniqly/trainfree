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
            setupFiles: ["./test/apply-migrations.js"],
            poolOptions: {
                workers: {
                    wrangler: { configPath: "./wrangler.jsonc" },
                    miniflare: {
                        bindings: {
                            TEST_MIGRATIONS: migrations,
                            // Stand in for the real per-caller secrets/vars the Access
                            // applications (task 9.3) provide in production; the
                            // audience and issuer values here match the constants the
                            // JWT/provider unit tests sign their test JWTs against.
                            ACCESS_TEAM_DOMAIN: "trainfree",
                            ADMIN_AUDIENCE: "trainfree-admin-audience",
                            WORKOUT_AUDIENCE: "trainfree-workout-audience",
                            ADMIN_INTERNAL_KEY: "test-admin-internal-key",
                            WORKOUT_INTERNAL_KEY: "test-workout-internal-key",
                            // Mirrors AdminApi's vitest.config.js -- exercises the actual
                            // deploy-stamped path, not just the local-build fallback.
                            APP_VERSION: "v9.9.9",
                            APP_COMMIT: "abc1234",
                        },
                    },
                },
            },
        },
    };
});
