# BB-104 — Universal Metadata-Aware Audiobook Search

## Metadata

- Date: 2026-08-24
- Scope: Open Library metadata-only vertical and existing Librarr discovery integration
- Sanitization notice: no keys, private addresses, provider payloads, tracker data, library data or credentials are published.

Detta är en sanerad GitHub-version; lokal runtime-evidens med privata värden publiceras inte.

## Status

**TECHNICALLY COMPLETE / DEPLOYED / RUNTIME VERIFIED.**

The original stop gate below is retained as decision history. On 2026-08-24 the owner explicitly approved Open Library as BigBrain's primary low-volume book metadata provider and authorized implementation, validation, documentation and publication.

## Requested capability

The requested slice would accept one universal query and resolve title, author, narrator, series, ISBN-10, ISBN-13 and probable ASIN into canonical provider-neutral book metadata. That evidence would drive a small deduplicated set of audiobook discovery queries while retaining BB-103's explicit edition selection and acquisition gates.

## Repository truth

BigBrain currently has no book metadata interface, resolver, configuration or HTTP adapter. `IAudiobookAcquisitionProvider` accepts title plus optional author and returns release candidates. Its Librarr implementation is bounded release discovery, not canonical work resolution.

Audiobookshelf exposes useful title, author, series, narrator, language, year and cover metadata for items already imported into the configured library. It cannot resolve arbitrary external ISBN/ASIN searches for works that are not present.

Librarr/Prowlarr and the approved native audiobook sources search releases. Their observed results commonly lack authoritative narrator, language, series and identifier data. The pinned Librarr source tree also contains an Open Library implementation under its ebook discovery registry. It is not exposed as a BigBrain metadata contract, is not part of the trusted audiobook discovery set and is not configured as a metadata service. Reusing it silently would cross the explicit provider/source-policy boundary.

No existing adapter can therefore reliably provide:

- canonical and alternate titles;
- structured authors;
- series and volume;
- verified narrators;
- ISBN-10/ISBN-13 crosswalk;
- ASIN crosswalk;
- metadata language/year/cover provenance for arbitrary works.

## Evidence

- Repository search found no `IAudiobookMetadataProvider`, equivalent metadata resolver, metadata-provider options or metadata HTTP client.
- The registered Media HTTP clients are Jellyfin, Sonarr, Radarr, Prowlarr, Audiobookshelf, Librarr and qBittorrent; none owns global book resolution.
- `IAudiobookshelfClient` maps series/narrator/language only from configured library items.
- `IAudiobookAcquisitionProvider.SearchAsync` accepts title, optional author and language and returns release candidates; ISBN/ASIN/series resolution is absent.
- The pinned Librarr Open Library implementation belongs to its ebook source registry and is excluded by the exact audiobook source policy.
- No code, test, build, container, configuration or external request was needed to prove the stop condition.

## Changes

- BigBrain owns `IAudiobookMetadataProvider`, normalized query/work contracts and the bounded discovery planner. Media is not coupled to Open Library types.
- The server-side Open Library adapter supports valid ISBN-10/13 and free-text lookup. It parses at most three works, has a five-second timeout, no automatic retry, a one-megabyte metadata-response cap and deterministic field bounds.
- Cover retrieval accepts numeric Open Library cover IDs only, proxies through BigBrain's same-origin API and caps content at two megabytes.
- The single mobile-first search field accepts title, author, series and ISBN. Probable ASIN is classified honestly but Open Library does not provide a reliable ASIN crosswalk.
- Metadata-first discovery produces at most two normalized seeds. The existing Librarr author-aware behavior expands these to at most six upstream source queries and retains partial-source success.
- Candidates are associated with sanitized metadata work ID/match evidence and deduplicated by opaque provider edition ID. Different editions, narrators and languages remain separate.
- Open Library is metadata-only. Release discovery, explicit edition selection, confirmation, acquisition, qBittorrent, no-overwrite import and Audiobookshelf reconciliation remain on the existing BB-102/103 path.

## Language and narrator behavior

Open Library language codes are normalized to BigBrain's `sv`, `en`, other supported ISO values or explicit `und`. Swedish remains a ranking preference. English and unknown candidates are never discarded merely because Swedish is preferred.

The contract contains narrator fields for future provider-neutral enrichment, but Open Library and the currently trusted discovery response do not reliably resolve narrator searches. Runtime capability is therefore `false`, the UI says this plainly and no narrator is fabricated. Adding narrator metadata requires another owner-approved provider decision.

## Historical stop decision

The initial prompt explicitly required stopping before introducing a new external metadata provider/API. That stop was honored and published locally before implementation. The subsequent owner decision approved Open Library only as metadata; it did not approve an acquisition source or any change to indexers, trusted Librarr sources, qBittorrent, audiobook files, Finance, Sentinel or BB-102/103 safety boundaries.

## Smallest owner decision

Review Open Library Books/Search API as the first metadata capability. It is the smallest credential-free public candidate for ISBN, title, author, edition and some series/cover evidence. Approval must still cover current API/usage terms, attribution, rate limits, bounded server-side caching, timeout/response-size limits, provenance and expected metadata gaps.

Narrator and ASIN coverage are not guaranteed by Open Library. If measured fixtures show those fields are insufficient, a separately reviewed secondary provider may be needed. Google Books is a possible later comparison candidate, but must not be enabled without its own terms, privacy, quota and credential decision.

Official Open Library guidance, reviewed 2026-08-24, describes the APIs as public, low-volume, human-facing discovery/lookup services rather than a third-party bulk backend. It requires responsible caching, recommends an identifying User-Agent/contact for regular use, and documents a default limit of one request per second or three requests per second for identified clients. The Search API supports bounded work/edition results and title/author queries; the API catalog also documents ISBN-oriented lookup and cover endpoints. Sources: [Open Library API guidelines](https://openlibrary.org/developers/api) and [Search API](https://openlibrary.org/dev/docs/api/search).

After approval, the smallest vertical implementation would be:

1. provider-neutral `IAudiobookMetadataProvider` and bounded resolved-work contract;
2. deterministic local ISBN-10/13 validation and honest free-text/probable-ASIN classification;
3. server-side metadata adapter with network-free fixtures;
4. bounded query planner with a hard fan-out limit;
5. existing Librarr discovery and BB-103 confirmation unchanged;
6. read-only title/author/narrator/ISBN runtime evaluation before any acquisition.

## Security

The stop preserves the exact pinned Librarr source policy, Prowlarr configuration, qBittorrent isolation, opaque candidate IDs, explicit owner confirmation, candidate expiry/single use, no-overwrite import, durable import evidence and Audiobookshelf reconciliation. Finance and Sentinel were not inspected or modified because they are outside this repository-only decision gate.

## Automated evidence

- Focused audiobook API tests: 59 passed.
- Complete API tests: 554 passed.
- Complete Web tests: 131 passed.
- Sentinel regression tests: 32 passed.
- Vite production and full .NET Release builds passed with no warnings.
- All Open Library fixtures are network-free; automated tests never create an acquisition.

## Runtime evidence

The API and Web were rebuilt and were the only services recreated. Dated read-only acceptance on 2026-08-24 found:

- `The Wandering Inn`, English preference: three Open Library work matches; the exact-title work was English and carried an ISBN, but current trusted discovery returned zero release candidates.
- `pirateaba`, English preference: three authored Open Library works; two bounded title-plus-author seeds were used and current trusted discovery returned zero release candidates.
- ISBN `9780261103573`, English preference: locally classified/normalized as ISBN-13, resolved to *The Fellowship of the Ring* by J.R.R. Tolkien and returned four distinct Prowlarr release candidates. Their languages remained explicit unknown because the provider supplied no authoritative language evidence.
- Planner fan-out remained at most two BigBrain seeds. Combined with Librarr bb4, a canonical title/author seed can produce title, title+author and leading-article-free title+author; normalized duplicates are suppressed. No `audiobook` suffix is duplicated.
- Acquisition jobs totaled 9 before deployment QA and 9 afterwards. No POST acquisition request or confirmation click occurred.
- Provider and Audiobookshelf states were `configuredHealthy`; the existing Audiobookshelf library still contained one item.
- Browser evidence covered 390×844, 430×932 and 1440×900 in Obsidian Gold, Forest Night and Arctic Wind. Metadata and four ISBN-derived release cards rendered without horizontal overflow. The final action scrolled fully above the mobile dock. Obsidian Gold was restored.
- Finance was read-only verified as `RESEARCH / 0 SEK / NONE`; scheduler enabled and not currently running. Sentinel tests passed, and its container was running after deployment.

## Remaining work

- reliable narrator resolution/search is unavailable;
- Open Library does not provide a complete ASIN crosswalk;
- metadata quality varies by work and edition;
- release availability remains limited by existing trusted Librarr/Prowlarr/native-source coverage.

No additional metadata provider or source is approved by BB-104.

## Resumption

A later narrator/ASIN enhancement must begin with a separate owner provider decision and must reuse `IAudiobookMetadataProvider`; it must not modify the acquisition boundary as a shortcut.
