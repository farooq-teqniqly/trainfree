// Defined once and shared by IdentityApi's role lookup and the provisioning
// script, so the two independent call sites can never disagree on the
// provider_name value that identifies a Cloudflare Access login.
export const PROVIDER_NAME_CLOUDFLARE_ACCESS = "cloudflare-access";
