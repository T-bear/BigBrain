# BB-122 — Historical Security Identity Evidence Pilot

Detta är en sanerad GitHub-version. No secret, credential, private address, account identifier,
raw market row, raw provider payload, raw log or sensitive filesystem path is published.

## Metadata

- Date: 2026-08-31
- Baseline/source of truth: `78736af9153ef28c435ca1a70b1971ae38677540`
- Scope: public-authority identity feasibility research over the retained WIKI artifact
- Finance boundary: `RESEARCH / 0 SEK / NONE`
- Result: **STATE B — EVIDENCE WORKS, BUT REQUIRES SUBSTANTIAL MANUAL RECONSTRUCTION**

## Status

BB-122 is documentation/research only. No WIKI download, candidate, mapping, promotion, canonical
mutation, source/schema/configuration change, autonomous run or deployment occurred. The existing
revision `wiki-5713d7dccfa38f56` remains immutable at 3,722 observations/five instruments. Stooq
remains `HUMAN CONFIRMATION REQUIRED / NO ACQUISITION` and was not revisited.

## Existing identity capability

`CanonicalInstrument` already represents an immutable instrument ID, type, name, currency, venue,
MIC, lifecycle and validity interval. `ProviderInstrumentMapping` adds provider/product/reference,
MIC, effective dates and an evidence reference. `InstrumentMappingCatalog` rejects overlapping
mappings and fails closed on missing or ambiguous date-specific resolution. Existing tests cover
MIC separation and ticker-change boundaries.

This is sufficient to express a time-bounded WIKI ticker mapping. It is not a historical security
master: it has no structured CIK/identifier field and no durable source-document/event evidence
model. The current WIKI intake also promotes only exact `EodhdCatalog.Watchlist` identities. A new
schema is not justified for a feasibility pilot; the evidence contract below remains research
evidence until a separately approved design can preserve it end to end.

## Deterministic cohort

The retained artifact was inspected read-only. After excluding AAPL, JNJ, JPM, MSFT and XOM, each
ticker's first session, last session and row count were calculated. Lexical ticker order selected
the first ten from each non-overlapping stratum: full snapshot coverage; ending before the snapshot
end; and starting after the snapshot start while reaching its end. This prevents selection of only
easy current survivors. Appearance is not classified as an IPO and disappearance is not classified
as a delisting.

| Stratum | Exact cohort (rows; first–last session) |
|---|---|
| Full | A (747), AAL (747), AAMC (734), AAN (747), AAOI (733), AAON (744), AAP (747), AAT (735), AAWW (745), ABAX (733); all 2014-01-02–2016-12-19 |
| Early end | ABFS (82; 2014-01-02–2014-04-30), ACCL (80; –2014-04-29), ACE (501; –2016-01-14), ACFN (390; –2015-07-23), ACI (506; –2016-01-11), ACO (88; –2014-05-08), ACW (725; –2016-11-17), ADNC (374; –2015-06-30), ADT (575; –2016-04-29), ADVS (380; –2015-07-07) |
| Late start | AA (34; 2016-11-01–2016-12-19), ADMS (666; 2014-04-10–2016-12-19), AGTC (677; 2014-03-27–2016-12-19), AHT (592; 2014-07-28–2016-12-19), AKAO (688; 2014-03-12–2016-12-19), AKBA (681; 2014-03-20–2016-12-19), ALDR (648; 2014-05-08–2016-12-19), AMBR (680; 2014-03-21–2016-12-19), ANTM (504; 2014-12-09–2016-12-19), ATEN (592; 2014-07-28–2016-12-19) |

## Authoritative sources and limitations

- [SEC developer resources](https://www.sec.gov/about/developer-resources) provide public EDGAR
  submissions, archives and APIs under fair-access rules: a declared user agent, efficient requests
  and no more than ten requests per second. Lawful discovery can be partly automated.
- [SEC filing search](https://www.sec.gov/search-filings) supplies historical company/form/CIK
  discovery. [Form 25-NSE technical specifications](https://www.sec.gov/submit-filings/technical-specifications)
  establish a structured filing path. A Form 25/25-NSE can prove a stated exchange/security removal
  event and effective-date semantics; it does not by itself prove why any WIKI ticker disappeared,
  every prior listing interval, ticker reuse, or corporate-action history.
- [Nasdaq Trader directory definitions](https://www.nasdaqtrader.com/trader.aspx?id=symboldirdefs)
  describe files updated during the current day, and [symbol lookup](https://www.nasdaqtrader.com/trader.aspx?comb=susq&id=symbollookup&mth=contains&typ=market)
  is explicitly current-trading-day evidence. No first-party free historical snapshot archive was
  established. Current membership cannot prove 2014–2016 membership.
- [NYSE corporate actions](https://www.nyse.com/market-data/corporate-actions) exposes current and
  upcoming event material, not a demonstrated complete free historical security master.
- SEC-hosted exchange filings can provide dated identity evidence. A
  [2016 exchange security list](https://www.sec.gov/Archives/edgar/vprr/1601/16019238.pdf) and an
  [older 2013 list](https://www.sec.gov/Archives/edgar/vprr/1303/13035241.pdf) identify many cohort
  ticker/name pairs. Such UTP/reference evidence proves dated identity presence, not necessarily
  principal listing venue or a complete validity interval.

## Minimal evidence contract

For each proposed mapping, evidence must retain: authority/provider; source document and content
identity where practical; publication, filing and stated effective dates; ticker; issuer/security
name; venue/MIC only where proven; CIK or another stable identifier only where proven; valid-from
and valid-to only where proven; event type; retrieval timestamp; provenance; status; and explicit
limitations. Status is one of `VERIFIED`, `PARTIAL`, `AMBIGUOUS` or `UNRESOLVED`. Missing evidence
never inherits a value from current directories or WIKI price coverage.

## Resolution matrix

“Candidate identity” is a research lead, not a canonical assertion. `—` means not proven by the
bounded authoritative review. General SEC exchange-list evidence is referenced above.

| Ticker | Candidate identity | Evidence | Status | Limitation |
|---|---|---|---|---|
| A | Agilent Technologies | Dated SEC identity; current SEC CIK 1090872/NYSE | PARTIAL | Full 2014–2016 venue interval not proven |
| AAL | American Airlines Group | Dated SEC identity; current SEC CIK 6201/Nasdaq | PARTIAL | Current directory cannot back-prove full interval |
| AAMC | Altisource Asset Management | Dated SEC identity | PARTIAL | Stable ID and complete venue interval not proven |
| AAN | Aaron's | SEC issuer evidence includes CIK 1038469 and dated ticker identity | PARTIAL | Complete venue/validity chain not proven |
| AAOI | Applied Optoelectronics | Dated SEC identity; current SEC CIK 1158114/Nasdaq | PARTIAL | Full interval not proven |
| AAON | AAON | Dated SEC identity; current SEC CIK 824142/Nasdaq | PARTIAL | Full interval not proven |
| AAP | Advance Auto Parts | Dated SEC identity | PARTIAL | Stable ID and full venue interval not proven |
| AAT | American Assets Trust | Dated SEC identity | PARTIAL | Stable ID and full venue interval not proven |
| AAWW | Atlas Air Worldwide | Dated SEC identity | PARTIAL | Later termination does not establish snapshot interval |
| ABAX | Abaxis | Dated SEC identity | PARTIAL | Later acquisition does not establish full snapshot interval |
| ABFS | Arkansas Best / ArcBest | SEC CIK 894405/Nasdaq; [name/symbol change effective 2014-05-01](https://www.sec.gov/Archives/edgar/data/894405/000110465914032756/a14-11039_1ex3d1.htm) | VERIFIED | Assertion ends with WIKI ABFS interval 2014-04-30 |
| ACCL | Accelrys | — | UNRESOLVED | No bounded authoritative chain completed |
| ACE | ACE Limited | SEC CIK 896159/NYSE; [Chubb name/CB trading from 2016-01-15](https://www.sec.gov/Archives/edgar/data/896159/000119312516430900/d113231d8k.htm) | VERIFIED | Scoped through WIKI ACE end 2016-01-14 |
| ACFN | Acorn Energy lead | — | UNRESOLVED | Identity/venue interval not established |
| ACI | Arch Coal lead | Conflicts with later reuse by another issuer | AMBIGUOUS | Requires time-bounded stable IDs |
| ACO | AMCOL International lead | Possible acquisition boundary not proven | AMBIGUOUS | Disappearance is not delisting evidence |
| ACW | Accuride lead | Possible transaction boundary not proven | AMBIGUOUS | Venue/stable ID/termination chain incomplete |
| ADNC | Audience lead | — | UNRESOLVED | No bounded authoritative chain completed |
| ADT | The ADT Corporation lead | Later ticker reuse by a different corporate period | AMBIGUOUS | Requires predecessor/successor adjudication |
| ADVS | Advent Software lead | — | UNRESOLVED | No bounded authoritative chain completed |
| AA | Alcoa Corporation | SEC CIK 1675149/NYSE; [AA trading from 2016-11-01](https://www.sec.gov/Archives/edgar/data/1675149/000119312516782709/R7.htm) | VERIFIED | Only 2016-11-01–snapshot-end is asserted |
| ADMS | Adamas Pharmaceuticals | Dated SEC identity lead | PARTIAL | Exact start, venue and stable-ID chain incomplete |
| AGTC | Applied Genetic Technologies | Dated SEC identity lead | PARTIAL | Exact start/full venue interval incomplete |
| AHT | Ashford Hospitality Trust | Dated SEC identity lead | PARTIAL | WIKI appearance is not listing evidence |
| AKAO | Achaogen lead | — | UNRESOLVED | Exact historical chain not completed |
| AKBA | Akebia Therapeutics | Dated SEC identity lead | PARTIAL | Exact start/full venue interval incomplete |
| ALDR | Alder BioPharmaceuticals | Dated SEC identity lead | PARTIAL | Exact start/full venue interval incomplete |
| AMBR | Amber Road lead | Foreign/private-transaction identity questions | AMBIGUOUS | Stable-ID/venue interval not established |
| ANTM | Anthem | SEC CIK 1156039/NYSE; [WellPoint→Anthem name change](https://www.sec.gov/Archives/edgar/data/1156039/000115603915000003/antm-20141231x10k.htm) | PARTIAL | Exact ANTM ticker-effective date not proven |
| ATEN | A10 Networks | Dated SEC identity lead | PARTIAL | Exact start/full venue interval incomplete |

## Evidence and feasibility decision

- `VERIFIED`: 3/30 (10.0%)
- `PARTIAL`: 17/30 (56.7%)
- `AMBIGUOUS`: 5/30 (16.7%)
- `UNRESOLVED`: 5/30 (16.7%)

SEC and exchange filings covered useful identity/event slices; current directories covered only
present state. Discovery/index retrieval is reasonably automatable under SEC fair-access rules,
but matching issuer events, principal venue, ticker reuse and complete validity intervals requires
document-specific human adjudication. The pilot was not timed per instrument, so no fabricated
duration is reported; observed effort ranged from one decisive filing to an unresolved trail. A
10% verified bounded result must not be extrapolated to all 3,181 ticker strings.

Conclusion: **STATE B**. Zero-cost public evidence can safely resolve some histories, but a reliable
universe requires substantial/manual reconstruction and is not presently reasonably automatable.

## Changes and promotion decision

No production code changed. No <=10-instrument mapping/promotion pilot occurred. Although three
symbols reached research-level `VERIFIED`, production cannot durably attach the full authority,
stable identifier, event and source-document chain to a WIKI promotion, and arbitrary-cohort
promotion semantics have not been architecturally approved. Mapping now would discard material
evidence semantics. All 13 gates remain unchanged; canonical before/after is 3,722/3,722.

## Security

Only bounded public-authority documents and aggregate local statistics were inspected. No key,
credential, private address/path, raw market row or provider payload is published. No paid or
protected route was used, no external message was sent and no owner data was changed.

## Tests and deployment

Documentation validation, link/BB-ID checks, Compose validation, whitespace checks and
secret-pattern review are the affected scope. Existing code tests remain authoritative because
production behavior did not change. Deployment: not applicable.

## Remaining work

- Decide whether the 10% verified yield and manual burden justify a separately designed,
  source-document-backed historical identity evidence model.
- If approved, define stable identifier/event persistence and exact candidate-scope semantics
  before any WIKI promotion. Current directories may only corroborate current state.
- Keep Stooq fail-closed until the owner supplies a first-party response.

## Resumption

The next action is an owner/system-architect decision on a narrow SEC/issuer-filing identity
evidence design. Do not promote cohort rows or build a generic security master from this report.
