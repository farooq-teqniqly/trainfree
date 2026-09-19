import { describe, expect, it } from "vitest";
import { parseProvisionArgs } from "./provision-args.js";

describe("parseProvisionArgs", () => {
    it("parses named flags", () => {
        expect(parseProvisionArgs(["--email", "a@b.c", "--role", "Administrator"])).toEqual({
            email: "a@b.c",
            roleName: "Administrator",
            remote: false,
        });
    });

    it("parses --remote", () => {
        expect(
            parseProvisionArgs(["--email", "a@b.c", "--role", "User", "--remote"]).remote,
        ).toBe(true);
    });

    // PowerShell swallows the `--` separator, so npm consumes --email/--role itself and
    // hands the script only the bare values (issue #136).
    it("falls back to positional email then role when flags are stripped", () => {
        expect(parseProvisionArgs(["a@b.c", "Administrator"])).toEqual({
            email: "a@b.c",
            roleName: "Administrator",
            remote: false,
        });
    });

    it("accepts a positional role alongside a named email", () => {
        expect(parseProvisionArgs(["--email", "a@b.c", "User"])).toMatchObject({
            email: "a@b.c",
            roleName: "User",
        });
    });

    it.each([
        ["an unknown flag", ["--nope", "a@b.c", "User"], /Unknown argument: --nope/],
        ["too many positionals", ["a@b.c", "User", "extra"], /Unexpected argument: extra/],
        ["a missing email", ["--role", "User"], /email is required/],
        ["a missing role", ["--email", "a@b.c"], /role is required/],
    ])("throws for %s", (_name, argv, message) => {
        expect(() => parseProvisionArgs(argv)).toThrow(message);
    });
});
