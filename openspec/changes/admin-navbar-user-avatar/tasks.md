## 1. Identity through the access gate

- [x] 1.1 Add an `EmailAddress` value object to `Trainfree.Domain` with its initial, tests-first; verify `Trainfree.Domain.Tests` pass
- [x] 1.2 Carry the caller's email and role on the `Administrator` outcome from `AccessCheck`; verify `AccessCheckTests` assert them
- [x] 1.3 Cascade the signed-in user from `AccessGate` to its content; verify `AccessGateTests` assert a child receives it

## 2. Navbar avatar

- [x] 2.1 Add a `UserAvatar` component (circle, initial, `title` and `aria-label` with the full email, role beneath, `data-testid`s); verify bUnit tests
- [x] 2.2 Render `UserAvatar` in `MainLayout` after the version indicator, styled in the scoped `UserAvatar.razor.css`; verify `MainLayoutTests` and a manual check in the browser at desktop and phone widths

## 3. Mockups and docs

- [x] 3.1 Add the avatar to the navbar of every screen in `docs/design/admin-mockups/`; verify by opening `Home.dc.html`
- [x] 3.2 Run `dotnet csharpier check .` and the full solution test suite in Release; verify both are clean
