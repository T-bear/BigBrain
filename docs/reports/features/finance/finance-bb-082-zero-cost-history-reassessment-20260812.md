# BB-082 zero-cost longer-history provider reassessment

## Metadata

- Date: 2026-08-12
- Baseline: `0f5a2990579bd1c55e2f179589f1c68988286491`
- Budget: 0 SEK
- Outcome: legitimate blocked path; no market data ingested
- Finance mode: `RESEARCH`

## Status

No investigated source currently provides a supported, zero-cost acquisition path that
materially extends BigBrain's eight-instrument US daily history while satisfying the
technical-access and retention gates. BB-082 therefore stopped before implementation. This
is not a finding that source-specific multi-provider architecture is unnecessary; it means
there is no lawful/reliable source to connect today.

Not implemented, not deployed and not manually runtime-verified because the legitimate
provider stop condition applied. Provider research and repository verification are complete.

## Evidence

Authoritative/public material was retrieved on 2026-08-12:

| Candidate | Access and depth | Cost / entitlement | Decision |
| --- | --- | --- | --- |
| Stooq | The pandas-datareader implementation maps directly to `https://stooq.com/q/d/l/` with US symbols such as `SPY.US`. Stooq pages expose long chart ranges and daily OHLCV conventions, but official adjustment/corporate-action and retention wording remains unclear. Both the terms route and a bounded SPY CSV request returned JavaScript proof-of-work/browser verification HTML, not terms or CSV. | Nominally public/0 SEK and capability-level `owner-accepted`; the active access control cannot be overridden. No raw redistribution is intended. | Blocked at technical access; no bypass, browser automation, alternate domain or hidden endpoint. |
| EODHD Free | Official pricing continues to describe free EOD access with approximately the past year and bounded daily calls. The active BigBrain account already supplies that legitimate window. | 0 SEK; existing owner-accepted active-account retention/deletion policy remains. | Usable current source, but cannot materially extend history without payment. No request spent. |
| Alpha Vantage Free | Official `TIME_SERIES_DAILY` documents 20+ years, raw OHLCV and CSV/JSON, but `outputsize=full` is premium-only; free `compact` is 100 points. Daily Adjusted is also identified as premium. | Free key does not unlock the required depth. | Rejected for BB-082 depth; no account/key/request. |
| Nasdaq Data Link | Official catalog classifies QuoteMedia End of Day US Prices (`EOD`) as premium; free platform datasets do not establish a qualifying current US equity/ETF OHLCV product. | Relevant product is paid and dataset rights are product-specific. | Rejected under 0 SEK; no account/key/request. |

Sources: [Stooq public site](https://stooq.com/),
[pandas-datareader Stooq adapter](https://github.com/pydata/pandas-datareader/blob/master/pandas_datareader/stooq.py),
[EODHD pricing](https://eodhd.com/pricing),
[Alpha Vantage API documentation](https://www.alphavantage.co/documentation/), and
[Nasdaq Data Link data organization](https://docs.data.nasdaq.com/docs/data-organization).

The Python wrapper is not entitlement evidence: its current source code calls Stooq's same
daily download route and defaults to a 20-year date range. No Python/R runtime is warranted.

### Repository and runtime verification

- Focused Finance backend: 170/170 passed; full backend: 384 API + 32 Sentinel passed.
- Web: 108/108 passed; .NET Release and Web production builds passed.
- Documentation: 141 Markdown files and 82 unique BB IDs passed; Compose configuration and
  `git diff --check` passed.
- Read-only existing runtime check: API and Web containers healthy; API returned EODHD Free,
  eight instruments, 2,016 durable observations over 2025-08-11–2026-08-11 and three
  separately persisted BB-081 evaluations, all still `INSUFFICIENT_DATA` in `RESEARCH`.
- Deployment/restart: not applicable because BB-082 changed no executable/runtime artifact.

## Request accounting and safety

- Market-data downloads: 0.
- Provider account/API requests: 0.
- Direct public research probes: 2 Stooq HTTP GETs (terms route and one SPY daily CSV route);
  both returned verification HTML and no market rows.
- Browser verification solutions, CAPTCHA workarounds, proxying, authentication bypass and
  endpoint reverse engineering: 0.
- Paid trials, subscriptions, payment details and cloud resources: 0.

No reliable provider payload was obtained, so price basis, volume semantics, session dates,
symbol coverage, checksums, per-instrument depth and overlap metrics cannot be truthfully
certified. Stooq's corporate-action basis remains `UNKNOWN`; values were not inferred.

### Preserved runtime evidence

BB-082 changed no executable code, database, payload directory, container or deployment.
The BB-078 EODHD memory (2,008-era observations and its append-only successors), BB-079
feature revisions, BB-080 backtests and BB-081 evaluations remain the source of truth.
No overlap comparison, long-history feature revision, backtest rerun or robustness
reevaluation was possible. Consequently `INSUFFICIENT_DATA` remains unchanged, not newly
recomputed. Existing EODHD daily acquisition and provider-specific deletion lineage are
unchanged.

## Changes

Only canonical status, backlog, roadmap, provider-selection and report/catalog documents
changed. No source, test fixture, database, provider payload, API, UI, Compose file or
runtime configuration changed.

### Architecture consequence

No new ADR is required. ADR 0021–0022 already require provider-neutral adapters,
capability-specific entitlement, exact provenance and fail-closed technical access. The
proposed durable rule that provider A evidence is not provider B evidence remains a future
implementation concern; BB-082 created no second provider revision and therefore did not
establish a new runtime reconciliation policy.

## Security

No access control was solved or bypassed. No credential, payment method, paid trial, raw
market row, account identity, provider response body or private runtime detail entered the
repository. Finance remains read-only `RESEARCH`; no broker, order, PAPER or LIVE capability
was added.

## Remaining work

Seek a normally supported Stooq bulk/API route with explicit automation and local-retention
clarity, without solving the browser challenge. If unavailable, investigate one named
government/exchange/open-data artifact whose license, US equity/ETF coverage and revision
provenance can be verified before access. A paid source requires a separate owner decision.
Do not add strategies or proceed to PAPER while historical evidence remains insufficient.

## Resumption

Start from this report, ADR 0021–0022 and the Finance provider-selection document. The
smallest safe next step is one supported-source entitlement/technical-access check; do not
implement an adapter until both gates pass.

## Sanitization

Detta är en sanerad GitHub-version. This report contains public URLs and aggregate request outcomes only. It contains no market
rows, provider secret, account identity, private address, raw response body or sensitive
filesystem path.
