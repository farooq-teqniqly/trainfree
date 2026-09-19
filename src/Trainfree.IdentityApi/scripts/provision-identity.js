// One-time provisioning CLI: adds a user's D1 identity (email + role) after the email
// is whitelisted in Cloudflare Access. Run against local D1 by default; pass --remote
// to target the deployed database (mirrors `wrangler d1 execute`'s own --local/--remote
// convention). Uses `getPlatformProxy` so it reads the same wrangler.jsonc binding
// IdentityApi's Worker uses at runtime, rather than a second, independently configured
// D1 connection. Local runs share AdminApi's local D1 persistence directory (see
// README.md's "Local development" section) since that's where the logins/users/roles
// tables actually get created.
//
// `--remote` reaches the real deployed database via wrangler.remote.jsonc, a dedicated
// config with `"remote": true` on the D1 binding. `getPlatformProxy`'s `remoteBindings`
// option only takes effect for a binding that itself declares `"remote": true` in the
// config file passed via `configPath`; the shared wrangler.jsonc deliberately does not
// set that (it would make plain `wrangler dev`/local `npm run provision` hit production
// D1 by default), so a local run keeps using the shared config while `--remote` switches
// to wrangler.remote.jsonc -- mirroring smoke-harness/wrangler.jsonc's separate-config
// pattern for the same reason.
import path from "node:path";
import { fileURLToPath } from "node:url";
import { getPlatformProxy } from "wrangler";
import { PROVIDER_NAME_CLOUDFLARE_ACCESS } from "../src/shared/providers.js";
import { parseProvisionArgs } from "../src/identity/provision-args.js";
import { provisionIdentity, UnknownRoleError } from "../src/identity/provisioning.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

async function main() {
    const { email, roleName, remote } = parseProvisionArgs(process.argv.slice(2));

    const configPath = remote
        ? path.join(__dirname, "wrangler.remote.jsonc")
        : path.join(__dirname, "..", "wrangler.jsonc");

    const { env, dispose } = await getPlatformProxy({
        configPath,
        persist: { path: path.join(__dirname, "..", "..", ".wrangler-shared", "v3") },
        remoteBindings: remote,
    });

    try {
        const result = await provisionIdentity(env.DB, {
            email,
            providerName: PROVIDER_NAME_CLOUDFLARE_ACCESS,
            roleName,
        });
        if (result.created) {
            console.log(`Provisioned ${email} as ${roleName} (${result.userId}).`);
        } else {
            console.log(`${email} is already provisioned; no changes made.`);
        }
    } finally {
        await dispose();
    }
}

main().catch((err) => {
    if (err instanceof UnknownRoleError) {
        console.error(err.message);
    } else {
        console.error("Provisioning failed:", err);
    }
    process.exitCode = 1;
});
