## REMOVED Requirements

### Requirement: IdentityApi is a service-binding-only third Worker
**Reason**: `Trainfree.IdentityApi` is deleted; identity resolution is now a stand-alone service shared by every app.
**Migration**: `AdminApi` calls cloudfare-identity over a service binding (see `admin-api-enforcement`).

### Requirement: IdentityApi binds the shared D1 database for identity tables only
**Reason**: The identity tables move out of Trainfree's database; cloudfare-identity owns its own store.
**Migration**: None for callers; the new database has no `logins`, `users` or `roles` tables (see `admin-api-enforcement`).

### Requirement: Identity schema migration rides AdminApi's existing migration history
**Reason**: Trainfree no longer owns an identity schema.
**Migration**: The new database starts from a single baseline migration with no identity tables; migrations 0014-0016 live on only in git history.

### Requirement: Provisioning script is idempotent and failure-safe
**Reason**: Users are provisioned in cloudfare-identity, not in Trainfree's database. The one existing user was loaded there with `userId` `USR-S6PHKJ` preserved.
**Migration**: Use cloudfare-identity's own provisioning runner.

### Requirement: Canonical provider_name constant is shared
**Reason**: There is no provisioning script or role lookup left in Trainfree to share a constant.
**Migration**: None.

### Requirement: JWT verification is independent of edge enforcement
**Reason**: Trainfree no longer verifies Access tokens; cloudfare-identity does, including the issuer check.
**Migration**: `AdminApi` forwards the token in `X-Identity-Token` (see `admin-api-enforcement`).

### Requirement: Audience check is against the named caller's specific audience
**Reason**: cloudfare-identity checks that the calling application's audience is in the token's `aud`.
**Migration**: `AdminApi` identifies itself with `X-Identity-Application: Trainfree.Admin`.

### Requirement: JWKS is fetched and cached via the Cloudflare Cache API
**Reason**: Signing-key fetching and caching moves with token verification to cloudfare-identity.
**Migration**: None.

### Requirement: Identity extraction is behind a provider seam
**Reason**: The seam lived inside the removed Worker.
**Migration**: None.

### Requirement: Service-binding contract for /internal/identity
**Reason**: The `/internal/identity` contract and its `X-Trainfree-*` headers are replaced by cloudfare-identity's `GET /v1/identity` and `X-Identity-*` headers.
**Migration**: See the `admin-api-enforcement` requirement on identity request headers. The `ADMIN_INTERNAL_KEY` value is reused unchanged.

### Requirement: Missing or wrong internal key responds 404, checked before the JWT
**Reason**: cloudfare-identity answers `404` for an unidentified caller itself.
**Migration**: `AdminApi` continues to treat that `404` as `503`.

### Requirement: Response shapes and status codes are stable JSON
**Reason**: The response contract is now owned by cloudfare-identity, which returns the same `{ email, userId, role }` shape plus optional `sessionId` and the same `401`/`403`/`404`/`503` statuses with `{ "error": string }` bodies.
**Migration**: `AdminApi` consumes the same shape (see `admin-api-enforcement`).

### Requirement: Role lookup matches provider_name and provider_id together
**Reason**: Role lookup moves to cloudfare-identity.
**Migration**: None.

### Requirement: Infrastructure failures surface as 503, never an uncaught 5xx
**Reason**: Owned by cloudfare-identity; `AdminApi` independently maps any non-`200`/`401`/`403` outcome to `503`.
**Migration**: See `admin-api-enforcement`'s identity service outage requirement.

### Requirement: Role lookup has no caching
**Reason**: Role lookup moves to cloudfare-identity.
**Migration**: None.

### Requirement: IdentityApi is stamped and polled in CI with no client banner
**Reason**: There is no `IdentityApi` deployment left to stamp or poll.
**Migration**: Remove its deploy and polling job from `deploy.yaml`.
