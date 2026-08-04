# Dashboard and Widget Framework Verification

- Status: Verified
- Scope: BigBrain Web only
- Risk: Low when runtime checks remain read-only

## Automated verification

From `src/BigBrain.Web` run `npm ci`, `npm test -- --run` and `npm run build`.
The tests cover registry composition, the four views, widget visibility, ordering,
collapse state, versioned persistence and fallback from malformed stored state.

## Responsive and manual verification

Verify Hem, Media, AI and Admin without a full page reload. On a narrow viewport,
confirm that bottom navigation does not cover content. On a wider viewport, confirm
the grid, spacing, edit controls and dialog focus. Placeholder widgets must be clearly
labelled as unavailable rather than appearing functional.

## Persistence and fallback

Change active view, order, visibility and collapsed state, then reload. The choices
must survive. Corrupt stored dashboard state only in an isolated test browser; the
application must fall back to defaults without losing module data.

## Web-only deployment

After green tests and explicit approval, run `docker compose up -d --no-deps --build web`.
Record container IDs and start times before and after. Only Web may change. This does
not authorize API recreation, external-service mutation or Compose edits.

## Rollback

Deploy the last known-good Web commit with the same Web-only command. Do not delete
browser storage as a first response because it is user state.

## Evidence

See the [sanitized Dashboard reports](../../reports/features/dashboard/) and
[ADR 0014](../../adr/0014-dashboard-views-and-widget-framework.md).
