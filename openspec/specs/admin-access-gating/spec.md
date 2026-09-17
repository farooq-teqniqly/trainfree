# admin-access-gating Specification

## Purpose

Gates the `Trainfree.Admin` app shell itself on the caller's role, so a non-Administrator
or unauthenticated browser never renders the normal app or fires any page's own API
calls while the check is in flight.

## Requirements

### Requirement: Root-level gate runs before the router renders any page
`Trainfree.Admin` SHALL call `GET /api/me` from a root component that wraps
`App.razor`'s `<Router>` -- not from a component nested inside a routed page or
`MainLayout` -- so that no matched page renders, and no matched page's own API calls
fire, until the gate has resolved.
**Rationale**: `VersionIndicator` renders inside `MainLayout`, after `<Router>` has
already rendered the matched page; reusing that placement for this check would let an
unauthorized user's browser briefly render the app shell and issue a protected page's
requests before the denial is known.

#### Scenario: Page does not render while the gate is pending
- **WHEN** `Trainfree.Admin` starts and `GET /api/me` has not yet resolved
- **THEN** no routed page or its `MainLayout` has rendered, and no page-level API call
  has been made

### Requirement: Administrator role renders the app normally
`Trainfree.Admin` SHALL render the normal app (the routed page inside `MainLayout`)
only when `GET /api/me` returns `200` with `role == "Administrator"`.
**Rationale**: `GET /api/me` succeeds with `200` for any provisioned identity,
Administrator or not (per `admin-api-enforcement`'s `GET /api/me returns any
provisioned identity's role` requirement), so the gate must inspect the role field
itself rather than treating any `200` as authorized.

#### Scenario: Administrator sees the normal app
- **WHEN** `GET /api/me` returns `200 { "email": "a@x.com", "role": "Administrator" }`
- **THEN** `Trainfree.Admin` renders the routed page inside `MainLayout`

#### Scenario: Provisioned non-Administrator is denied
- **WHEN** `GET /api/me` returns `200 { "email": "u@x.com", "role": "User" }`
- **THEN** `Trainfree.Admin` renders the "no access" page, not the normal app

### Requirement: 401/403 responses show a "no access" page
`Trainfree.Admin` SHALL render a "no access" page, instead of the normal app, when
`GET /api/me` returns a `401` or `403` response, regardless of the response body.
**Rationale**: Both statuses mean "you don't get in" -- `401` because the request
couldn't be authenticated at all, `403` because it authenticated to an unprovisioned
identity -- and the end-user experience of either is identical: this app is not for
you. The status code alone is decisive; the gate does not need to inspect the body to
classify these two statuses, so an intermediary that answers a `401`/`403` with a
non-JSON body (e.g. a plain edge error page) is still "no access," not a fourth,
distinct outcome.

#### Scenario: Unauthenticated caller sees "no access"
- **WHEN** `GET /api/me` returns a `401` response
- **THEN** `Trainfree.Admin` renders the "no access" page

#### Scenario: Unprovisioned identity sees "no access"
- **WHEN** `GET /api/me` returns a `403` response
- **THEN** `Trainfree.Admin` renders the "no access" page

### Requirement: Network failure or 5xx shows a distinct retry state
`Trainfree.Admin` SHALL render a "something went wrong, retry" state, distinct from the
"no access" page, when the `GET /api/me` call fails at the transport level (no response
reaches the app) or the response status is `5xx`.
**Rationale**: Folding an infrastructure outage into "no access" would misinform an
Administrator into thinking they lack permission when the real problem is that
`AdminApi` or `IdentityApi` is down; `admin-api-enforcement`'s own `503` requirement
exists to keep this distinction available to the client.

#### Scenario: Network failure shows retry, not no-access
- **WHEN** the `GET /api/me` call throws a transport-level exception (no HTTP response
  received)
- **THEN** `Trainfree.Admin` renders the "something went wrong, retry" state, not the
  "no access" page

#### Scenario: 5xx response shows retry, not no-access
- **WHEN** `GET /api/me` returns a `503` (or any `5xx`) response
- **THEN** `Trainfree.Admin` renders the "something went wrong, retry" state, not the
  "no access" page

### Requirement: A non-JSON response forces a top-level reload, not a fetch retry
`Trainfree.Admin` SHALL treat a `GET /api/me` response that is neither a valid
"authorized" body nor a valid `401`/`403` JSON body -- for example a response that
fails to parse as JSON -- as a fourth, distinct outcome, and SHALL force a top-level
browser navigation reload (e.g. `NavigationManager.NavigateTo(..., forceLoad: true)` or
`location.reload()`), rather than rendering "no access" or "something went wrong" or
retrying with another `fetch` call.
**Rationale**: `GET /api/me` is a `fetch`-style same-origin call, not a top-level
navigation; on an expired Access session, Cloudflare Access answers with its own HTML
login page (still same-origin, still some HTTP status) instead of the expected JSON, so
parsing throws -- `VersionCheck.cs:58-70` already documents this exact failure shape for
the same underlying cause. Only a real top-level navigation lets Access's edge redirect
to its login page and re-authenticate the session; classifying this response as
"no access" would misinform a still-valid Administrator whose session merely expired,
and classifying it as "retry" would loop forever since a plain retry is still a `fetch`.

#### Scenario: Non-JSON response triggers a top-level reload
- **WHEN** `GET /api/me` returns a response whose body cannot be parsed as the expected
  JSON shape (mirroring `VersionCheck`'s `JsonException`/`InvalidOperationException`/
  `NotSupportedException` catch)
- **THEN** `Trainfree.Admin` forces a top-level navigation reload instead of rendering
  either the "no access" page or the "something went wrong" state

#### Scenario: Reload is a real navigation, not another fetch
- **WHEN** the non-JSON-response outcome is handled
- **THEN** the app issues a top-level browser navigation (forced reload), not another
  `HttpClient`/`fetch` call to `GET /api/me`
