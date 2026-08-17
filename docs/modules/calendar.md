# Kalender module

## Scope and status

Kalender is BigBrain's shared time-domain module. Heroma is a server-side import adapter, not the module identity. The MVP is implemented and automatically verified on 2026-08-05; production deployment and user-controlled import verification are recorded separately in `docs/STATUS.md`.

## Domain contract

Calendar events own local `date`, nullable local `startTime`/`endTime`, event type, visual classification, sanitized source label, source/import identity and audit timestamps. Work shifts remain local wall-clock values; they are not converted through UTC. `endsNextDay` represents overnight shifts. Other modules may read data only through the versioned Calendar API or a future published module contract, never through Calendar tables.

Event types are `work`, `education`, `collaboration`, `vacation` and `other`. Work visualization is `day`, `evening` or `unknown`. Heroma source text is normalized, bounded and retained only when safe.

## API v1

- `GET /api/v1/modules/calendar/week`
- `GET /api/v1/modules/calendar/month?year=YYYY&month=M`
- `GET /api/v1/modules/calendar/imports`
- `POST /api/v1/modules/calendar/import-preview` using multipart `.xlsx` files
- `POST /api/v1/modules/calendar/imports/{previewId}/confirm`

Preview never mutates persistence. Confirm accepts `add`, `replace`, `merge` or `cancel`. Problems use stable `calendarImport*` codes and sanitized details.

## Persistence and retention

Calendar owns SQLite tables `CalendarEvents` and `CalendarImports` in the API data volume. Import confirm is serialized and transactional. Raw workbooks are parsed from a bounded request stream and discarded; they are not persisted, logged or returned. Import history contains only sanitized filename, SHA-256 identity, detected month, counts, parser version and status.

## Duplicate and conflict rules

An exact file hash plus parser version cannot be confirmed twice. Replacing removes only Heroma-sourced events in the selected month. Merge skips exact normalized identities and rejects differing work shifts on an already occupied date. Cancel makes no change. The model's `source` boundary protects future manual events from Heroma replacement.

## Presentation

The stable `calendar` widget ID replaces the former placeholder. Home shows the current week. The expanded modal provides Swedish month navigation, direct time display, a seven-column desktop grid, a mobile list, import and import history. Symbols always have text: ☀️ day, 🌙 evening, 📚 education, 🚗 collaboration and 🏖️ vacation.

The expanded dialog is one scroll container. Its header, toolbar and content use vertical flex/block flow; the complete desktop grid or mobile month list precedes Import history in DOM and layout order. Mobile reserves bottom space for fixed navigation and history filenames wrap inside their cards. Calendar days and history must not use absolute positioning.

## Known limitations

The MVP supports the verified single-month Swedish calendar-grid `.xlsx` format. It does not support PDF/OCR, encrypted workbooks, ICS/Google/Apple sync, manual events, per-user calendars, reminders or deletion of prior imports. See BB-029.

Calendar remains a module presented within Home, not a standalone BigBrain view. Accessible visual
recession of past days and mobile swipe navigation are future UX investigations; neither behavior is
implemented or approved as a final interaction specification. See the [planning record](../reports/documentation/product-ux-auth-school-meals-backlog-capture-20260817.md).
