# worker-trace-context Specification

## Purpose

Gives Worker traces in the Cloudflare dashboard application context (who made the
request and which login it belongs to) so they can be filtered by user and grouped by
session, instead of every HTTP request being an anonymous, unrelated trace.

## Requirements

### Requirement: Root spans carry user.id and user.role once identity resolves
`AdminApi` and `IdentityApi` SHALL each set `user.id` (the internal `userId`, never the
email) and `user.role` as attributes on their active span once the caller's identity is
resolved. A request that fails before identity resolves (missing binding or key, `401`,
`403`, `404`, `503`) SHALL carry no user attributes.
**Rationale**: Filtering by user is the point of the change; the internal ID keeps PII
out of a telemetry store, and an unresolved request has no honest value to attach.
Both Workers set the attributes because cross-Worker trace propagation over the
service binding is unconfirmed.

#### Scenario: Resolved identity tags the span
- **WHEN** a request's identity resolves to userId `u1` with role `Administrator`
- **THEN** the Worker's active span has `user.id = "u1"` and `user.role = "Administrator"`
- **AND** no span attribute contains the email or any token

#### Scenario: Failure before identity resolution leaves the span untagged
- **WHEN** a request fails before identity resolves (for example the deployed
  `ADMIN_INTERNAL_KEY` is absent and the Worker responds `503`)
- **THEN** no `user.*` or `session.id` attribute is set

### Requirement: Root spans carry a server-derived session.id
`IdentityApi` SHALL derive a `sessionId` from the verified Access JWT's per-login
`identity_nonce` claim as a one-way value of 32 lowercase hex characters, set it as
`session.id` on its span, and return it to the caller. `AdminApi` SHALL set the same
value as `session.id` on its own span. The raw nonce SHALL never appear in a span, log,
or response. Two requests under the same Access login SHALL yield the same `session.id`;
requests under different logins of the same user SHALL yield different values.
**Rationale**: Every request is its own trace, so a stable per-login key is the only
way to list one browsing session's traces together. Deriving it server-side from the
already-verified JWT means no client change and no untrusted input; hashing keeps a
lookup key for Access's identity record out of telemetry.

#### Scenario: Same login yields the same session.id across Workers
- **WHEN** two requests carry JWTs from one login (same `identity_nonce`)
- **THEN** the `session.id` on `IdentityApi`'s and `AdminApi`'s spans is identical for
  both requests

#### Scenario: A new login yields a different session.id
- **WHEN** the same user logs in again and the new JWT has a different `identity_nonce`
- **THEN** the resulting `session.id` differs from the earlier login's

#### Scenario: Missing nonce omits session.id without failing the request
- **WHEN** a verified JWT has no `identity_nonce` claim
- **THEN** the request succeeds normally and no `session.id` attribute is set

### Requirement: Telemetry attribution never affects request outcomes or authorization
Setting trace attributes SHALL NOT throw or alter any response, including when there is
no active span. `session.id` SHALL be used for observability only and never for any
authorization or access decision.
**Rationale**: The custom span API is in beta; a telemetry failure must not become a
user-visible outage or a trust boundary.

#### Scenario: No active span
- **WHEN** there is no active span (for example under `wrangler dev` or in a test)
- **THEN** attribute-setting is a no-op and the response is unchanged

#### Scenario: Local dev bypass
- **WHEN** `LOCAL_DEV_BYPASS` is set
- **THEN** `AdminApi` sets `user.id` and `user.role` from the synthetic identity, sets no
  `session.id` (there is no JWT), and local dev behavior is otherwise unchanged

## Decisions

- Chose a server-derived `session.id` from the Access JWT's `identity_nonce` over a
  client-generated GUID sent as `X-Session-Id`. Two decoded real tokens (same user,
  two logins) showed `sub` constant and `identity_nonce` differing, so it is per-login.
  The client GUID would be right if per-tab granularity is ever needed, or if Access
  stops issuing the claim; it costs a Blazor `DelegatingHandler`, `sessionStorage`
  interop, header forwarding, a CORS allow-header, and validation of untrusted input.
- Accepted login-level (about 24h, all tabs) granularity: Access tokens last 24h
  (`exp - iat = 86400`) and one login spans tabs. Filtering by time range narrows it.
- `identity_nonce` is not a documented-stable Cloudflare contract, so a missing claim
  omits `session.id` rather than failing; the `sessionId` response field is optional.
- `IdentityApi` returns `sessionId` in its body rather than `AdminApi` decoding the JWT,
  so JWT/claim knowledge stays inside the identity provider seam.
- Tracing enablement (`observability.traces`) is already on in all three Worker configs;
  no config change is part of this change.

## Requirement coverage

Anchor: GitHub issue #151 (feat(observability): attach user.id and session.id to Worker traces)

| # | Anchor requirement | Covered by |
|---|--------------------|-----------|
| 1 | Enable tracing in wrangler configs | Not covered - already enabled in `wrangler.jsonc`, `wrangler.deploy.jsonc`, and IdentityApi's config; see Decisions |
| 2 | AdminApi sets user.id / user.role after identity resolves | Req: Root spans carry user.id and user.role once identity resolves |
| 3 | IdentityApi sets user.* after JWT validation and lookup | Req: Root spans carry user.id and user.role once identity resolves |
| 4 | Blazor DelegatingHandler sending X-Session-Id | Not covered - superseded by server-derived session.id; see Decisions |
| 5 | AdminApi validates X-Session-Id and sets session.id | Req: Root spans carry a server-derived session.id (no client header to validate) |
| 6 | Forward session id over IDENTITY binding; IdentityApi sets session.id | Req: Root spans carry a server-derived session.id (IdentityApi derives, returns sessionId) |
| 7 | CORS: allow X-Session-Id | Not covered - no new client header; see Decisions |
| 8 | vitest: no active span does not throw | Req: Telemetry attribution never affects request outcomes (Scenario: No active span) |
| 9 | vitest: malformed or oversized X-Session-Id ignored | Req: Root spans carry a server-derived session.id (Scenario: Missing nonce omits session.id); client input no longer exists |
| 10 | Verify in prod: filter on session.id lists one session across both Workers | tasks.md prod-verification task |
| 11 | Use userId not email; no secrets in attributes | Req: Root spans carry user.id and user.role (Scenario: Resolved identity tags the span) |
| 12 | X-Session-Id is untrusted, never used for authorization | Req: Telemetry attribution never affects request outcomes or authorization |
| 13 | Pre-identity failures carry no user | Req: Root spans carry user.id and user.role (Scenario: Failure before identity resolution) |
| 14 | AC: prod traces from both Workers carry user.id and session.id | Reqs: user.id/user.role; server-derived session.id |
| 15 | AC: all traces from one session listable together | Req: Root spans carry a server-derived session.id (same-login scenario) |
| 16 | AC: local dev unaffected | Req: Telemetry attribution... (Scenarios: No active span, Local dev bypass) |
