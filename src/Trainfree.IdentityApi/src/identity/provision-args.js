/**
 * Parses the provisioning CLI's arguments.
 *
 * Named flags (`--email`, `--role`, `--remote`) are the documented form. Bare
 * positionals (email, then role) are accepted as a fallback because Windows PowerShell
 * swallows the `--` that `npm run provision --` relies on: npm then treats `--email`/
 * `--role` as its own unknown config and forwards only their values.
 *
 * @param {string[]} argv Arguments after the script name.
 * @returns {{ email: string, roleName: string, remote: boolean }}
 */
export function parseProvisionArgs(argv) {
    const args = { remote: false };
    const positionals = [];

    for (let i = 0; i < argv.length; i += 1) {
        const arg = argv[i];
        if (arg === "--email") {
            args.email = argv[++i];
        } else if (arg === "--role") {
            args.roleName = argv[++i];
        } else if (arg === "--remote") {
            args.remote = true;
        } else if (arg.startsWith("--")) {
            throw new Error(`Unknown argument: ${arg}`);
        } else {
            positionals.push(arg);
        }
    }

    for (const value of positionals) {
        if (!args.email) {
            args.email = value;
        } else if (!args.roleName) {
            args.roleName = value;
        } else {
            throw new Error(`Unexpected argument: ${value}`);
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
