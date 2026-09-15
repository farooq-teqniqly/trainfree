// One-time provisioning CLI: adds a user's D1 identity (email + role) after the email
// is whitelisted in Cloudflare Access. Run against local D1 by default; pass --remote
// to target the deployed database (mirrors `wrangler d1 execute`'s own --local/--remote
// convention). Uses `getPlatformProxy` so it reads the same wrangler.jsonc binding
// IdentityApi's Worker uses at runtime, rather than a second, independently configured
// D1 connection.
import path from "node:path";
import { fileURLToPath } from "node:url";
import { getPlatformProxy } from "wrangler";
import { PROVIDER_NAME_CLOUDFLARE_ACCESS } from "../src/shared/providers.js";
import { provisionIdentity, UnknownRoleError } from "../src/identity/provisioning.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

function parseArgs(argv) {
    const args = { remote: false };
    for (let i = 0; i < argv.length; i += 1) {
        const arg = argv[i];
        switch (arg) {
            case "--email":
                args.email = argv[++i];
                break;
            case "--role":
                args.roleName = argv[++i];
                break;
            case "--remote":
                args.remote = true;
                break;
            default:
                throw new Error(`Unknown argument: ${arg}`);
        }
    }

    if (!args.email) {
        throw new Error("--email is required");
    }
    if (!args.roleName) {
        throw new Error("--role is required");
    }

    return args;
}

async function main() {
    const { email, roleName, remote } = parseArgs(process.argv.slice(2));

    const { env, dispose } = await getPlatformProxy({
        configPath: path.join(__dirname, "..", "wrangler.jsonc"),
        experimental: { remoteBindings: remote },
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
