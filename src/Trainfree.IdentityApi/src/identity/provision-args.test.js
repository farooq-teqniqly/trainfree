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

    it.each([
        ["an unknown flag", ["--nope", "a@b.c", "User"], /Unknown argument: --nope/],
        ["a positional role after a named email", ["--email", "a@b.c", "User"], /^Do not mix positional and named arguments$/],
        ["a positional email after a named role", ["--role", "User", "a@b.c"], /^Do not mix positional and named arguments$/],
        ["too many positionals", ["a@b.c", "User", "extra"], /Unexpected argument: extra/],
        ["a missing email", ["--role", "User"], /^Error: --email is required$|^--email is required$/],
        ["a missing role", ["--email", "a@b.c"], /^--role is required$/],
        ["--email followed by another flag", ["--email", "--role", "User"], /^--email requires a value$/],
        ["--email followed by --remote", ["--email", "--remote", "User"], /^--email requires a value$/],
        ["--role followed by --remote", ["a@b.c", "--role", "--remote"], /^--role requires a value$/],
        ["an empty --email value", ["--email", "", "User"], /^--email requires a value$/],
        ["a blank --role value", ["a@b.c", "--role", " "], /^--role requires a value$/],
        ["a blank positional email", [" ", "User"], /^Positional arguments must not be blank$/],
        ["an empty positional role", ["a@b.c", ""], /^Positional arguments must not be blank$/],
        ["a dangling --email", ["a@b.c", "User", "--email"], /^--email requires a value$/],
        ["a dangling --role", ["a@b.c", "--role"], /^--role requires a value$/],
    ])("throws for %s", (_name, argv, message) => {
        expect(() => parseProvisionArgs(argv)).toThrow(message);
    });
});
