// Per-caller configuration for the /internal/identity service-binding contract. Each
// entry names the env var holding that caller's internal-key secret and the env var
// holding that caller's Access application audience -- kept as a lookup table (not a
// switch) so validating a `X-Trainfree-Caller` value is just "is it a key in here."
export const CALLER_CONFIG = {
    admin: { internalKeyVar: "ADMIN_INTERNAL_KEY", audienceVar: "ADMIN_AUDIENCE" },
    workout: { internalKeyVar: "WORKOUT_INTERNAL_KEY", audienceVar: "WORKOUT_AUDIENCE" },
};

// The Access team domain is the only non-secret piece of Access config; issuer and
// certs URL are both derived from it so the two can never drift apart.
export function issuerFor(env) {
    return `https://${env.ACCESS_TEAM_DOMAIN}.cloudflareaccess.com`;
}

export function certsUrlFor(env) {
    return `${issuerFor(env)}/cdn-cgi/access/certs`;
}
