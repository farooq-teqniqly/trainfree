## ADDED Requirements

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
