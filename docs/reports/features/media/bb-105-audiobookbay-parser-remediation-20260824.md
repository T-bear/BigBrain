# BB-105 — AudioBookBay Parser & Literal Author Search Remediation

## Metadata

- Date: 2026-08-24
- Scope: pinned AudioBookBay parser boundary and literal author-query preservation
- Sanitization notice: no credentials, source URLs, result paths, download identifiers, private addresses or raw provider payloads are published.

Detta är en sanerad GitHub-version; lokal runtime-evidens med privata värden publiceras inte.

## Status

**TECHNICALLY COMPLETE / DEPLOYED / RUNTIME VERIFIED / CI PENDING.**

## Scope and root cause

BB-105 is the bounded remediation approved after the read-only audiobook discovery diagnostic. It changes neither the pinned Librarr upstream revision nor source/indexer/acquisition/import architecture.

The approved AudioBookBay mirror returned nine search posts for the author identity used in the diagnostic. Each had a valid title link. Current markup places the text `English` immediately before a nested metadata-label element. Upstream used the flattened goquery `.Text()` value, producing `EnglishKeywords:` and rejecting every row as non-English. Network reachability, title selectors and the catalogue itself were not the cause.

## Changes

`bigbrain-librarr:1208254-bb6` applies `0006-audiobookbay-parser-boundaries.patch` after the five existing reviewed patches. It joins adjacent `.postInfo` DOM nodes with a separator before reading `Language:`. The fixture contains sanitized invented titles, two English rows with the exact nested-label boundary and one non-English control. Parser diagnostics are debug-level counts only: posts, titled rows, language rejections and retained rows.

BigBrain reserves an author-only literal input before metadata-derived work identities. The maximum remains two provider seeds and six upstream Librarr variants. Language ranking now follows the selected mode: Swedish or English is preferred, while unknown remains visible; All Languages has no preference. No result is removed solely because its language is unknown.

## Security

- Pinned upstream commit and revision-bound trusted source set: unchanged.
- Prowlarr/indexers and qBittorrent: unchanged.
- Explicit edition review and acquisition confirmation: unchanged.
- Opaque candidate IDs and bounded response: unchanged.
- No-overwrite, conflict, traversal, import evidence and Audiobookshelf completion rules: unchanged.
- No source URL, query, title, download identifier or credential appears in parser logs.
- No acquisition endpoint was called during runtime QA.

## Evidence

- Focused BigBrain audiobook tests: 48 passed.
- Complete API suite: 555 passed.
- Complete Web suite: 131 passed.
- Complete Sentinel suite: 32 passed.
- Librarr image gate: organizer, search and download packages plus focused API passed; the search package includes the fixture regression.
- Release build: succeeded with zero warnings/errors.
- Vite production build: succeeded.

## Sanitized runtime evidence

For literal author query `pirateaba`:

| Stage | Count |
| --- | ---: |
| Source HTML posts | 9 |
| AudioBookBay parsed rows | 9 |
| Librarr retained rows | 9 |
| BigBrain final candidates | 9 |

All nine displayed candidates retained sanitized `AudioBookBay` provenance and remained honestly `und/unknown`; English preference did not remove them. `The Wandering Inn`, `Wandering Inn`, `The Wandering Inn` plus author and naturally discovered `Fae and Fare` remained zero-result searches. The remediation therefore materially improves author discovery but does not invent title matching that the source does not provide.

At 390×844, Obsidian Gold, Forest Night and Arctic Wind each rendered nine candidates with no horizontal overflow. The last action scrolled fully above the dock, and no confirmation was opened. Provider and Audiobookshelf reported `configuredHealthy`; the existing library count remained one. Acquisition jobs were 10 before and after QA.

## Remaining work

- AudioBookBay's site-side title search still returns no useful result for the tested title variants; author search is currently the effective identity.
- Current releases lack authoritative structured language and narrator metadata, so candidates remain unknown rather than being guessed English.
- Current Prowlarr coverage still returns zero for this series.
- CI evidence is recorded after publication.

## Resumption

No operational continuation is required for BB-105. A future discovery-quality slice should begin from `bigbrain-librarr:1208254-bb6`, retain all six patches and re-run the fixture plus complete import/no-overwrite regressions. Do not add sources or modify acquisition behavior without a separate owner decision.
