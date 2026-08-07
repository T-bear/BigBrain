# ADR 0015: Calendar ownership and Heroma import boundary

- Status: Proposed
- Date: 2026-08-05

## Context

BigBrain needs a durable calendar while the first source is a private monthly Heroma workbook. Long-lived decisions concern module ownership, parser placement, raw-file retention, preview/confirm, duplicate identity and month replacement.

## Proposed decision

Kalender owns the time-domain model and persistence; Heroma is an adapter. Parsing occurs server-side with ClosedXML behind strict file, workbook, sheet, row and event limits. The browser never becomes the authoritative parser. Raw files live only for the bounded request parse and are discarded. Document properties are ignored.

Preview stores only normalized events in short-lived API memory and does not mutate. Confirm is transactional. Exact identity is SHA-256 plus parser version. Replace deletes only Heroma-sourced events in the detected month. Merge skips exact normalized events and rejects different work shifts on one date. Cancel is inert.

Calendar local dates and times remain wall-clock values; overnight state is explicit. Future modules read through versioned API/module contracts, not Calendar tables. Manual and other future sources are distinguished by `source` and are outside Heroma replacement scope.

## Security and operational consequences

The API accepts `.xlsx` only, validates signature/MIME/size and caps files, sheets, rows and events. Macros are never executed. Logs contain counts and error codes, never cell contents, local paths or hashes. The database persists in a dedicated least-privilege volume.

## Alternatives rejected

Client-only parsing would create an untrusted duplicate authority. Permanent raw-file retention has no MVP need. PDF/OCR is less deterministic than the supplied format. Replacing all calendar events would violate future source ownership.

## Rollback and limitations

Rollback redeploys the prior API/Web while preserving the Calendar volume. No destructive import deletion endpoint is included. The decision covers only the verified Swedish monthly grid; calendar sync, reminders, manual events and per-user ownership require later decisions.
