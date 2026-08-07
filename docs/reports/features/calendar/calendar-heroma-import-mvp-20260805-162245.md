# Calendar and Heroma import MVP

Detta är en sanerad GitHub-version. Lokala identiteter, interna adresser, råloggar och hemligheter har utelämnats.

## Metadata

- Date: 2026-08-05 (Europe/Stockholm)
- Initial HEAD: `932aae009e8b49d8714ccdb44c65dfbdd0400bcf`
- Scope: BB-028, Calendar module and Heroma `.xlsx` import MVP
- Backlog: BB-028
- ADR: Proposed ADR 0015

## Status

Implementation, full automated quality gates and the scoped API/Web deployment are complete. After restart, the product owner verified the current work week and imported schedule data as correct. The separate mobile month-layout fix passed 94 Web tests and a production build, was deployed Web-only, and awaits final product-owner verification. No commit or push has occurred.

## Evidence

The local sample was verified as OOXML `.xlsx`, one Swedish month sheet in an eight-column calendar grid with weekday headers, merged title only, local 24-hour ranges, specialty labels and multiple ranges on one date. Macros and external links were absent. Document-property values were not printed and are ignored by the parser.

The sanitized parser mapping uses sheet month/year, day number plus weekday-column validation, explicit education/collaboration/vacation terms, work fallback for timed entries and `other` for unknown non-free text. No explicit day/evening code was verified, so parser version v1 uses start before 12:00 for day and start at/after 12:00 for evening. Local times are not converted through UTC.

Automated evidence: .NET restore and Release build succeeded with zero warnings/errors; 203 API tests and 32 Sentinel tests passed; 91 Web tests and the Vite production build passed. Documentation verification covered 82 Markdown files and 29 unique BB IDs. Compose validation, diff check and repository hygiene passed.

Deployment evidence: only API and Web were rebuilt/recreated. Both became healthy; the unrelated Sentinel and FlareSolverr container identities were unchanged. API health, Calendar week/import-history and Web returned HTTP 200. Calendar and import counts were zero, proving no real schedule import occurred during deployment.

Mobile-fix deployment evidence: only Web was rebuilt/recreated and became healthy with HTTP 200. API, Sentinel and FlareSolverr retained their pre-deployment container identities. No schema import or API mutation was performed.

## Changes

- Backend: Calendar domain contracts, ClosedXML Heroma adapter, bounded multipart preview, short-lived normalized previews, transactional confirm and Calendar-owned SQLite persistence.
- API: versioned week, month, imports, import-preview and confirm endpoints with sanitized Problem Details.
- Frontend: stable `calendar` widget replacing the placeholder, compact week, expanded Swedish desktop/mobile month, direct times, import preview/results and history.
- Mobile layout correction: the dialog's four-row CSS grid incorrectly constrained the month list to a `minmax(0,1fr)` row while Import history occupied the next row, allowing visible day overflow to collide with history. Calendar content now uses explicit vertical flex/block flow; the full month precedes history, long names wrap and mobile bottom padding accounts for fixed navigation.
- Duplicate model: SHA-256 plus parser version, normalized event identity, source-scoped replacement and conflict-rejecting merge.
- Deployment: Compose adds only the API-owned `calendar-data` volume; expected services are API and Web.
- Documentation: module, knowledge, runbooks, Proposed ADR, status, backlog, indexes, testing and this report.

## Security

Files are limited to `.xlsx`, checked for MIME, OOXML signature and size, and bounded by request file count, sheet count, rows and events. Raw bytes live only in request memory and are discarded after parsing. Macros are never executed. Raw cells, hashes, local paths and document properties are not logged. Original private files are outside the repository and covered by defensive ignore rules. Fixtures are synthetic.

## Remaining work

- Product owner performs the real multi-file import, duplicate, replace/merge/cancel and mobile verification.
- Product owner verifies the corrected 320–430 px month flow, especially August 12–15, history placement, long-card wrapping, final-content padding and full scrolling.
- After successful manual verification: update status/report, classify and stage only Calendar files, commit, push, verify `HEAD == origin/main` and inspect CI.
- Known product limitations: verified monthly Swedish `.xlsx` grid only; no PDF/OCR, import DELETE, external sync, manual/family events, reminders, AI suggestions or per-user calendar.

## Resumption

```text
Repository: BigBrain main
Initial implementation HEAD: 932aae009e8b49d8714ccdb44c65dfbdd0400bcf
Work item: BB-028
ADR: Proposed 0015
Current phase: deployed; stopped for product-owner manual verification
Do not import the private workbook by terminal or automation.
Preserve unrelated Sentinel ADR changes exactly as found.
Next: complete quality gates, deploy api/web only, then stop for product-owner manual verification.
Commit/push only after that verification succeeds.
```
