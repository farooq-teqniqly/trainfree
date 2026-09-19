## ADDED Requirements

### Requirement: Navbar user identity
The admin app's top navbar SHALL display the signed-in user's identity in its top-right,
after the version indicator: an avatar rendered as a circle whose content is the
uppercased first letter of the user's email address, with the user's role shown beneath
the avatar. Hovering the avatar SHALL reveal the user's full email address as a tooltip.
The avatar SHALL also expose the full email address to assistive technology.
**Rationale**: The app is gated to a single provisioned identity, so showing who is
signed in and in what role is the cheapest confirmation that the right Access session is
live; the initial keeps the navbar compact while the tooltip preserves the full address.

#### Scenario: Avatar shows the email's first letter
- **WHEN** the signed-in administrator's email is `farooq@example.com` and any admin page
  loads
- **THEN** the navbar shows a circular avatar containing `F`

#### Scenario: Initial is uppercased
- **WHEN** the signed-in administrator's email begins with a lowercase letter
- **THEN** the avatar's initial is that letter in uppercase

#### Scenario: Hover reveals the full email
- **WHEN** the user hovers the avatar
- **THEN** a tooltip shows the full email address

#### Scenario: Role appears beneath the avatar
- **WHEN** any admin page loads
- **THEN** the navbar shows the user's role, `Administrator`, directly beneath the avatar

#### Scenario: Identity coexists with the version indicator
- **WHEN** any admin page loads
- **THEN** the navbar shows the version indicator exactly once, and the avatar to its
  right

## Decisions

- Carried the identity from the existing `GET /api/me` response through the access gate
  to the layout rather than adding a second identity request. The gate already fetches
  and validates it once at startup; a second call would duplicate the round trip and
  could disagree with the gate's own decision. A separate call would be right only if
  the identity ever needed refreshing without a reload.
- Used the native `title` attribute for the tooltip rather than Bootstrap's tooltip
  component. The app loads Bootstrap CSS only (no JS bundle, CDN-pinned with SRI), and a
  native tooltip needs none. Bootstrap's tooltip would be right if styling or keyboard-
  focus reveal became a requirement; the avatar exposes an `aria-label` meanwhile.
- The role shown is the role the gate admitted. Only `Administrator` passes the gate
  today, so the text is always `Administrator`; it is carried as data rather than
  hardcoded so a future second permitted role renders correctly.
- The mockups in `docs/design/admin-mockups/` are updated to include the avatar; the live
  Claude Design canvas is not republished by this change.

## Requirement coverage

Anchor: intent.md (frozen, this change directory)

| # | Anchor requirement | Covered by |
|---|--------------------|-----------|
| 1 | Circle avatar with the first letter of the user's email | Req: Navbar user identity (Avatar shows the email's first letter, Initial is uppercased) |
| 2 | Full email in a tooltip on hover | Req: Navbar user identity (Hover reveals the full email) |
| 3 | User's role under the avatar | Req: Navbar user identity (Role appears beneath the avatar) |
