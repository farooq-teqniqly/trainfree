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
    // A flag's value must not itself look like a flag: `--email --role User` would
    // otherwise bind email to "--role" and write a bogus identity instead of failing.
    const takeValue = (flag, index) => {
        const value = argv[index];
        if (value === undefined || value.trim() === "" || value.startsWith("--")) {
            throw new Error(`${flag} requires a value`);
        }
        return value;
    };

    const args = { remote: false };
    const positionals = [];

    for (let i = 0; i < argv.length; i += 1) {
        const arg = argv[i];
        if (arg === "--email") {
            args.email = takeValue(arg, ++i);
        } else if (arg === "--role") {
            args.roleName = takeValue(arg, ++i);
        } else if (arg === "--remote") {
            args.remote = true;
        } else if (arg.startsWith("--")) {
            throw new Error(`Unknown argument: ${arg}`);
        } else {
            positionals.push(arg);
        }
    }

    // The positional form exists only for the case where npm stripped both flags; mixing
    // it with a named flag makes the binding order-dependent, so refuse it.
    if (positionals.length > 0 && (args.email || args.roleName)) {
        throw new Error("Do not mix positional and named arguments");
    }

    for (const value of positionals) {
        if (value.trim() === "") {
            throw new Error("Positional arguments must not be blank");
        }
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
