# Calendar verification runbook

## Automated gates

Run backend restore/build/test, frontend `npm ci`, `npm test -- --run` and `npm run build`, documentation verification, `git diff --check` and `docker compose config --quiet`. Fixtures must be synthetic workbooks and cover parsing, special types, multiple entries, overnight time, invalid structure, persistence, exact duplicate, replace and merge conflict.

## Manual production plan

The product owner performs the real import; Codex must not upload the private workbook.

1. Confirm Kalender is visible on Hem and Matlista/Inköpslista remain in place.
2. Confirm the current week, Swedish dates, labels, symbols and times.
3. Open expanded Calendar; test previous/next month and direct times.
4. Select at least two private monthly files and inspect preview per file.
5. Confirm import and compare dates, times and classifications with the source.
6. Reload and confirm persistence.
7. Reimport an exact file and confirm no duplicate.
8. Test Replace, Merge and Cancel for an existing month; confirm a conflict is not overwritten.
9. Verify mobile month list without horizontal scrolling.
10. Report the result before commit/push approval is exercised.

## BB-028 mobile overlap regression

After restart, the product owner verified the current work week and imported schedule data as correct. A separate mobile defect placed Import history over approximately 12–15 August. The fix replaces the dialog's constrained content grid row with a vertical flow.

Verify at 320–430 px width:

1. All August days are visible and scroll in order.
2. Days 12–15 do not overlap Import history.
3. Import history follows the final month day.
4. Long filenames wrap inside their cards.
5. Fixed bottom navigation does not cover the final card.
6. Escape, close focus restoration and import dialog behavior remain intact.

After the clicks, read-only masked API logs and aggregate database counts may be inspected. Never print source labels or individual schedule values.
