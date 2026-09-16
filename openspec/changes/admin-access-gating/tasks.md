## 1. Outcome type and gate check

- [ ] 1.1 Add `AccessCheckOutcome` (`Administrator` / `NoAccess` / `AccessCheckFailed` /
      `ReauthenticationRequired`) as a closed `abstract record` hierarchy in
      `src/Trainfree.Admin`, mirroring `Trainfree.Versioning.VersionCheckOutcome`'s
      shape. Verify the project builds and each case is a distinct sealed record with no
      shared mutable/nullable fields.
- [ ] 1.2 Add `IAccessCheck`/`AccessCheck` calling `GET api/me` via `HttpClient`, mapping
      `200 { role: "Administrator" }` to `Administrator`, `200` with any other role or a
      JSON `401`/`403` to `NoAccess`, transport failure or `5xx` to `AccessCheckFailed`,
      and a response that fails to parse as the expected JSON (mirroring
      `VersionCheck`'s `JsonException`/`InvalidOperationException`/
      `NotSupportedException` catch) to `ReauthenticationRequired`. Verify with unit
      tests covering all four outcomes plus the `200`/non-Administrator-role case, using
      a fake `HttpMessageHandler` per `CLAUDE-baseline.md`'s test-handler pattern.
- [ ] 1.3 Register `IAccessCheck` and its `HttpClient` in `Program.cs` against the same
      base address the other Admin API clients use. Verify `dotnet build` succeeds and
      the app starts locally.

## 2. Root-level gate component

- [ ] 2.1 Add an `AccessGate` component that wraps `<Router>`'s content: on
      `OnInitializedAsync` it calls `IAccessCheck`, renders nothing (or a minimal
      loading state) while pending, and branches on the outcome once resolved. Verify
      with a bUnit test asserting the router's content is not rendered before the check
      resolves.
- [ ] 2.2 Update `App.razor` to nest `<Router>` inside `AccessGate`, replacing the
      top-level `<Router>` render. Verify the app still renders the matched page for the
      `Administrator` outcome (bUnit test) and that no routed page or its `MainLayout`
      renders for any other outcome.
- [ ] 2.3 Add a `NoAccessPage` component/page shown for the `NoAccess` outcome, distinct
      from `AdminApi`'s JSON error body -- plain "you don't have access to this app"
      messaging, no navigation into the app shell. Verify with a bUnit test that it
      renders on `NoAccess` and contains no link into a protected route.
- [ ] 2.4 Add an `AccessCheckErrorPage` component/page shown for the `AccessCheckFailed`
      outcome, with retry messaging distinct from `NoAccessPage`'s wording. Verify with a
      bUnit test that its rendered text differs from `NoAccessPage`'s and that it is
      reachable only from `AccessCheckFailed`.
- [ ] 2.5 Wire the `ReauthenticationRequired` outcome to force a top-level reload
      (`NavigationManager.NavigateTo(uri, forceLoad: true)` or equivalent) instead of
      rendering any page. Verify with a unit/bUnit test asserting `NavigateTo` is called
      with `forceLoad: true` and that no further `IAccessCheck`/`HttpClient` call is made
      for this outcome.

## 3. Integration verification

- [ ] 3.1 Run the full bUnit suite for `Trainfree.Admin.Tests` and confirm all four
      `AccessGate` outcome paths (Administrator, NoAccess, AccessCheckFailed,
      ReauthenticationRequired) are covered by at least one test, per the spec's
      `## Requirement coverage` table.
- [ ] 3.2 Manually verify against local `wrangler dev` (`LOCAL_DEV_BYPASS` synthetic
      Administrator identity) that the app renders normally, and against a
      forged/incorrect JWT or missing bypass that the `NoAccess` page renders instead of
      the app shell.
