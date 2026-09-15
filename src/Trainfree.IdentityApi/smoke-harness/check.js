// Rollout smoke check (tasks 9.4/10.2): confirms /internal/identity resolves a real
// provisioned identity through the actual service binding to the deployed IdentityApi
// Worker -- see wrangler.jsonc's comment for why this config, and not AdminApi's own
// local wrangler.jsonc, is what makes the check meaningful. Mirrors
// scripts/provision-identity.js's use of `getPlatformProxy` with `remoteBindings` to
// reach a real deployed binding from a plain Node script, no `wrangler dev` needed.
import path from "node:path";
import { fileURLToPath } from "node:url";
import { getPlatformProxy } from "wrangler";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

function toFlagName(argName) {
    return `--${argName.replace(/[A-Z]/g, (c) => `-${c.toLowerCase()}`)}`;
}

function parseArgs(argv) {
    const args = {};
    for (let i = 0; i < argv.length; i += 1) {
        const arg = argv[i];
        switch (arg) {
            case "--internal-key":
                args.internalKey = argv[++i];
                break;
            case "--jwt":
                args.jwt = argv[++i];
                break;
            case "--expect-email":
                args.expectEmail = argv[++i];
                break;
            default:
                throw new Error(`Unknown argument: ${arg}`);
        }
    }

    for (const name of ["internalKey", "jwt", "expectEmail"]) {
        if (!args[name]) {
            throw new Error(`${toFlagName(name)} is required`);
        }
    }

    return args;
}

async function main() {
    const { internalKey, jwt, expectEmail } = parseArgs(process.argv.slice(2));

    const { env, dispose } = await getPlatformProxy({
        configPath: path.join(__dirname, "wrangler.jsonc"),
        experimental: { remoteBindings: true },
    });

    try {
        // The URL's host is arbitrary -- service bindings route on the binding, not the
        // URL -- but the path must be the real route IdentityApi's Worker handles.
        const response = await env.IDENTITY.fetch("https://trainfree-identity-api/internal/identity", {
            headers: {
                "X-Trainfree-Caller": "admin",
                "X-Trainfree-Internal-Key": internalKey,
                Cookie: `CF_Authorization=${jwt}`,
            },
        });

        const body = await response.json().catch(() => null);

        // A bare 200 isn't proof this went through the real binding -- assert the real
        // email the provisioning script created came back, not a synthetic value.
        if (response.status !== 200 || body?.email !== expectEmail) {
            console.error(`Smoke check FAILED: status=${response.status} body=${JSON.stringify(body)}`);
            process.exitCode = 1;
            return;
        }

        console.log(`Smoke check PASSED: ${body.email} resolved as ${body.role} (${body.userId}).`);
    } finally {
        await dispose();
    }
}

main().catch((err) => {
    console.error("Smoke check errored:", err);
    process.exitCode = 1;
});
