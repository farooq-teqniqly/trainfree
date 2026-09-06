# admin-shell Specification

## Purpose
The admin app's shared shell -- navbar, sidebar navigation, and the Home landing
page -- that every admin page renders within.

## Requirements
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
- **THEN** the sidebar lists exactly `Home`, `Phases`, `Exercises`, and `Programs`,
  in that order -- no `Admin` link

#### Scenario: Active link is highlighted
- **WHEN** the admin user is on the Programs page
- **THEN** the sidebar's `Programs` link is styled as active and the `Home`,
  `Phases`, and `Exercises` links are not

### Requirement: Home landing page
The admin app's root route (`/`) SHALL render a tile-grid landing page. Each tile
SHALL name a library or management area; a tile for an area with an existing page
SHALL link to it, and a tile for an area without a page yet SHALL render without a
link.

#### Scenario: Programs tile links to the Programs page
- **WHEN** the admin user clicks the `Programs` tile on the Home page
- **THEN** the app navigates to `/programs`

#### Scenario: Phases tile links to the Phases page
- **WHEN** the admin user clicks the `Phases` tile on the Home page
- **THEN** the app navigates to `/phases`

#### Scenario: Exercises tile links to the Exercises page
- **WHEN** the admin user clicks the `Exercises` tile on the Home page
- **THEN** the app navigates to `/exercises`

### Requirement: Mobile nav toggler exposes its expanded state to assistive tech
The admin app's mobile nav toggler button SHALL expose its expanded/collapsed state
via a literal `aria-expanded="true"` or `aria-expanded="false"` string attribute --
never an HTML boolean toggle (present-with-empty-value or omitted).

#### Scenario: Toggler starts collapsed
- **WHEN** any admin page loads on a narrow viewport
- **THEN** the nav toggler button renders `aria-expanded="false"`, reflecting the
  sidebar's actual collapsed rendering

#### Scenario: Clicking the toggler flips the reported state
- **WHEN** the admin user clicks the nav toggler button
- **THEN** the button's `aria-expanded` attribute value changes to `"true"` and the
  sidebar expands

