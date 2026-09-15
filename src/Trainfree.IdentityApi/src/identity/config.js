// Per-caller configuration for the /internal/identity service-binding contract. Each
// entry names the env var holding that caller's internal-key secret and the env var
// holding that caller's Access application audience -- kept as a lookup table (not a
// switch) so validating a `X-Trainfree-Caller` value is just "is it a key in here."
// `X-Trainfree-Caller` is attacker-controlled input used directly as the lookup key, so
// this is a null-prototype object: a plain `{}` literal would let a caller send
// `X-Trainfree-Caller: constructor`/`toString`/`__proto__` and get back a truthy
// inherited `Object.prototype` value instead of `undefined`, passing the "is it
// configured" check with a value that has no `internalKeyVar`/`audienceVar`.
export const CALLER_CONFIG = Object.assign(Object.create(null), {
    admin: { internalKeyVar: "ADMIN_INTERNAL_KEY", audienceVar: "ADMIN_AUDIENCE" },
    workout: { internalKeyVar: "WORKOUT_INTERNAL_KEY", audienceVar: "WORKOUT_AUDIENCE" },
});

// The Access team domain is the only non-secret piece of Access config; issuer and
// certs URL are both derived from it so the two can never drift apart.
export function issuerFor(env) {
    return `https://${env.ACCESS_TEAM_DOMAIN}.cloudflareaccess.com`;
}

export function certsUrlFor(env) {
    return `${issuerFor(env)}/cdn-cgi/access/certs`;
}
