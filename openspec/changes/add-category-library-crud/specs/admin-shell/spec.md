## MODIFIED Requirements

### Requirement: Sidebar navigation
The admin app SHALL display a fixed-width sidebar listing only the pages that
currently exist, with no wrapper link for the app's own admin-ness (the whole app is
the admin app). The active page's link SHALL be visually distinguished.

#### Scenario: Sidebar shows only implemented pages
- **WHEN** any admin page loads
- **THEN** the sidebar lists exactly `Home`, `Categories`, and `Programs` -- no `Admin`
  link, no `Exercises` link (that ships when its page exists)

#### Scenario: Active link is highlighted
- **WHEN** the admin user is on the Programs page
- **THEN** the sidebar's `Programs` link is styled as active and the `Home` and
  `Categories` links are not

### Requirement: Home landing page
The admin app's root route (`/`) SHALL render a tile-grid landing page. Each tile
SHALL name a library or management area; a tile for an area with an existing page
SHALL link to it, and a tile for an area without a page yet SHALL render without a
link.

#### Scenario: Programs tile links to the Programs page
- **WHEN** the admin user clicks the `Programs` tile on the Home page
- **THEN** the app navigates to `/programs`

#### Scenario: Categories tile links to the Categories page
- **WHEN** the admin user clicks the `Categories` tile on the Home page
- **THEN** the app navigates to `/categories`

#### Scenario: Not-yet-built tiles render without a link
- **WHEN** the Home page loads
- **THEN** the `Exercises` tile renders with its name and description but is not
  clickable/navigable to any route
