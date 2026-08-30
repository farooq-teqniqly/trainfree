## ADDED Requirements

### Requirement: Navbar brand and version indicator
The admin app's top navbar SHALL display a fixed-width brand block reading
"Trainfree Admin" with an icon mark, and SHALL display the running version (via
`VersionIndicator`) exactly once, in the navbar's top-right. No other page location
SHALL duplicate the version display.

#### Scenario: Navbar renders on every page
- **WHEN** any admin page loads
- **THEN** the navbar shows the "Trainfree Admin" brand block and the version
  indicator in the top-right

#### Scenario: Version indicator appears exactly once
- **WHEN** the Home page loads
- **THEN** the running version is displayed only in the navbar -- no second copy
  appears in the page body

### Requirement: Sidebar navigation
The admin app SHALL display a fixed-width sidebar listing only the pages that
currently exist, with no wrapper link for the app's own admin-ness (the whole app is
the admin app). The active page's link SHALL be visually distinguished.

#### Scenario: Sidebar shows only implemented pages
- **WHEN** any admin page loads
- **THEN** the sidebar lists exactly `Home` and `Programs` -- no `Admin` link, no
  `Categories` or `Exercises` link (those ship when their pages exist)

#### Scenario: Active link is highlighted
- **WHEN** the admin user is on the Programs page
- **THEN** the sidebar's `Programs` link is styled as active and the `Home` link is not

### Requirement: Home landing page
The admin app's root route (`/`) SHALL render a tile-grid landing page. Each tile
SHALL name a library or management area; a tile for an area with an existing page
SHALL link to it, and a tile for an area without a page yet SHALL render without a
link.

#### Scenario: Programs tile links to the Programs page
- **WHEN** the admin user clicks the `Programs` tile on the Home page
- **THEN** the app navigates to `/programs`

#### Scenario: Not-yet-built tiles render without a link
- **WHEN** the Home page loads
- **THEN** the `Categories` and `Exercises` tiles render with their name and
  description but are not clickable/navigable to any route
