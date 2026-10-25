## MODIFIED Requirements

### Requirement: Root spans carry user.id and user.role once identity resolves
`AdminApi` SHALL set `user.id` (the internal `userId`, never the email) and `user.role`
as attributes on its active span once the caller's identity is resolved. A request that
fails before identity resolves (missing binding or credential, an identity service
`401`/`403`, `404`, `503`) SHALL carry no user attributes. A caller whose identity does
resolve but whom `AdminApi` then rejects with its own role-based `403` (a `User` on an
Administrator-only endpoint) is still tagged, since the identity did resolve.
**Rationale**: Filtering by user is the point of the change; the internal ID keeps PII
out of a telemetry store, and an unresolved request has no honest value to attach.
Only `AdminApi` sets the attributes because the identity service is a separate
deployment with its own telemetry.

#### Scenario: Resolved identity tags the span
- **WHEN** a request's identity resolves to userId `u1` with role `Administrator`
- **THEN** the Worker's active span has `user.id = "u1"` and `user.role = "Administrator"`
- **AND** no span attribute contains the email or any token

#### Scenario: Failure before identity resolution leaves the span untagged
- **WHEN** a request fails before identity resolves (for example the deployed
  `ADMIN_INTERNAL_KEY` is absent and the Worker responds `503`)
- **THEN** no `user.*` or `session.id` attribute is set

### Requirement: Root spans carry a server-derived session.id
`AdminApi` SHALL set the `sessionId` returned by the identity service as `session.id` on
its own span. The identity service derives it from the verified Access token's per-login
claim as a one-way value of 32 lowercase hex characters. The raw claim SHALL never appear
in a span, log, or response. Two requests under the same Access login SHALL yield the
same `session.id`; requests under different logins of the same user SHALL yield different
values.
**Rationale**: Every request is its own trace, so a stable per-login key is the only
way to list one browsing session's traces together. Deriving it server-side from the
already-verified token means no client change and no untrusted input; hashing keeps a
lookup key for Access's identity record out of telemetry.

#### Scenario: Same login yields the same session.id across Workers
- **WHEN** two requests carry tokens from one login
- **THEN** the `session.id` on `AdminApi`'s span is identical for both requests

#### Scenario: A new login yields a different session.id
- **WHEN** the same user logs in again and the new token carries a different per-login
  claim
- **THEN** the resulting `session.id` differs from the earlier login's

#### Scenario: Missing nonce omits session.id without failing the request
- **WHEN** the identity service returns a `200` with no `sessionId`
- **THEN** the request succeeds normally and no `session.id` attribute is set

#### Scenario: Failed derivation omits session.id without failing the request
- **WHEN** the identity service cannot derive a `sessionId` and returns a `200` without it
- **THEN** `AdminApi` resolves the identity normally and sets no `session.id`

#### Scenario: Malformed sessionId is dropped by AdminApi
- **WHEN** the identity service returns a `200` whose `sessionId` is not 32 lowercase hex
  characters
- **THEN** `AdminApi` still resolves the identity, sets `user.id` and `user.role` but no
  `session.id`, and does not respond `503`
