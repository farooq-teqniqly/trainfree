// The identity provider seam. Cloudflare Access is the only registered provider in
// this change; every call site outside `./providers/cloudflare-access.js` imports
// `extractIdentity` from here rather than reaching into a specific provider module or
// parsing Access claims (email/aud/iss) directly.
export { extractIdentity } from "./providers/cloudflare-access.js";
