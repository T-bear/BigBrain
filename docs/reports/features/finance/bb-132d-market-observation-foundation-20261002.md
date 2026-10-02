# BB-132D — Real market observation foundation

Detta är en sanerad GitHub-version. No credentials, raw provider payloads, private addresses or model context.

## Metadata

Baseline fetched from accepted main: `17dc0ac0fba1869a14905acb2176669860cfac62`, tree
`aab75e7776adfbd4e581dc4db1342e5bf155c339`. Branch
`bb-132d/real-market-observation-foundation`. Accepted A/B/C grants and runtime stay unchanged.
Goal: bounded provider-independent acquired observations, immutable local evidence and knowledge-cutoff
replay. No prospective evaluator, trading, scheduler, model, deployment or BB-132E.

## Characterization before implementation

- `MarketData.cs`: Instrument/MarketVenue/Price/Currency/Candle and UTC validation are reusable.
  `IMarketDataSource` is a basic quote/candle port; its response owns ObservedAtUtc and lacks a
  trusted persistence receipt/correction/knowledge contract. Do not pretend adapter time is trusted.
- `CanonicalMarketData.cs`: effective-dated canonical instrument/provider/product/MIC mappings,
  decimal normalization and raw/adjusted distinction exist. Reuse identities rather than ticker-only keys.
- `LiveMarketObservation` already separates event/provider/received/knowledge times, but requires
  a known provider timestamp and caller-supplied stream/revision/journal identity. Missing provider
  availability cannot be silently invented to fit that constructor. Its feed/shadow pipeline is synthetic.
- ADR0029 and `FinanceShadowResearch` already persist EODHD-derived prospective predictions and
  outcomes with acquisition/cutoff lineage; `FinanceProspectiveCadence` gates its own EOD polling.
  Those existing provider-specific prediction/evaluation responsibilities are NOT reimplemented or
  connected to D. They do not provide the new provider-independent snapshot acquisition receipt
  and sealed knowledge projection. Existing cadence/shadow code remains unchanged.
- `HistoricalReplay`/immutable dataset assembly have explicit availability/revision semantics for
  supplied evidence. They do not attest when a network response was actually acquired by this host.
- `EodhdMarketMemory` owns the existing Finance SQLite connections; despite its historical name,
  partials already own scientific results, learning governance and finite sessions. `FinanceSchemaMigrator`
  owns additive versions through95, using BEGIN IMMEDIATE and transactional rollback. Reuse that owner.
- EODHD `observations` retain acquired_utc and immutable payload-derived revision identity; its latest
  snapshot is a current/historical display view, not a sealed prospective knowledge replay. Its adapter
  is EOD/provider-specific and cannot be relabelled as prospective acquisition or Twelve Data transport.
- `FinanceBacktestPersistence`/RobustnessStore retain immutable native results/checksums. They remain
  untouched; no new scientific result store/evaluator is needed. Source/knowledge-time no-lookahead
  and A/B/C histories remain outside this observation-only work.
- Existing adapters live in Api/Finance behind narrow domain contracts. Reuse placement and accepted
  configuration/secret binding; no new service, provider SDK or runtime registration.
- ADR0021/0022/0028 require affirmative per-use/storage/retention rights. Historical Twelve Data
  Personal permission does NOT authorize Basic. No live ingestion is implied by this checkpoint.
  Existing backups enumerate approved historical sources; new observation evidence must not enter
  those backups implicitly. Provider-specific retention/deletion support remains a live-use gate.

## Status

**MERGE CANDIDATE / IMPLEMENTED / AUTOMATICALLY VERIFIED / REVIEW CANDIDATE ONLY.**
Not accepted, merged, deployed or live-provider verified. All D fixture acceptance criteria and local gates pass.

## Bounded design direction

Use the existing Finance owner with an additive receipt table and durable knowledge-cutoff watermark.
Adapter values are untrusted; Finance assigns acquisition/ingestion times through trusted TimeProvider.
Unknown provider publication time stays explicitly unknown; knowledge cannot precede local receipt.
A query may seal only a cutoff no later than trusted now. A later new receipt must be strictly after
that watermark; clock rollback cannot insert data into an already observed past. Transactions serialize
this rule across connections. Exact duplicates preserve the original receipt; conflicts fail closed;
explicit correction lineage never edits prior evidence. Historical stores are not imported/backfilled.
Initial observation scope is bounded OHLCV snapshots, not a finalized trading bar or scientific
eligibility claim. Existing instrument/MIC/currency identities and price validation are reused.
Full provider semantics and tests are required before calling this implemented/verified.

## Evidence

Final implementation verification on2026-10-02 (all model-free):

| Command / check | Result |
| --- | --- |
| Pre-change API characterization filter EodhdIntegration/Historical/MarketData/LiveObservationLearning/DatasetRevisionAssembly, Release `--no-build` |122 PASS,0 failures |
| `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --configuration Release --filter 'FullyQualifiedName~MarketObservation'` |56 PASS,0 failures; final observation/provider cases |
| API Release filter `FullyQualifiedName~MarketObservation\|FullyQualifiedName~FinanceLearningLedger\|FullyQualifiedName~FiniteResearch\|FullyQualifiedName~FinanceDatasetRevisionAssembly\|FullyQualifiedName~FinanceHistorical\|FullyQualifiedName~FinanceShadow\|FullyQualifiedName~FinanceEodhd` |186 PASS; followed by final provenance guard test above and full suite below |
| `dotnet restore BigBrain.slnx` |PASS, dependencies already current |
| `dotnet build BigBrain.slnx --configuration Release --no-restore` |PASS,0 warnings/0 errors |
| `BB132A_LOCAL_ACCEPTANCE=disabled BB132C_LOCAL_ACCEPTANCE=disabled dotnet test BigBrain.slnx --configuration Release --no-build` |API975 PASS/0 FAIL/8 intentional real-model SKIP; Sentinel32 PASS/0 FAIL |
| `dotnet format BigBrain.slnx --verify-no-changes --no-restore` |PASS, no changes |
| `node scripts/verify-documentation.mjs` |PASS,261 Markdown/91 unique backlog IDs, including final evidence update |
| `git diff --check` |PASS, including staged candidate |
| Gitleaks8.28.0 `git . --redact --no-banner` |PASS,307 commits, no leaks |
| Gitleaks8.28.0 `git . --pre-commit --staged --redact --no-banner` |PASS, no leaks |
| Private A/C evidence manifest SHA-256 comparison |A18/C11 entries unchanged; no new inference/session/grant |
| Exact intended scope vs baseline |20 files; no frontend, Sentinel, Brain, native worker, package, CI, provider activation or deployment change |

Initial development failures were corrected in D: analyzers required small code-style/cancellation
adjustments; migration96 required explicit reader compatibility and moving two unsupported-version
fixtures from96 to999. The final checks above passed. Initial sandbox MSBuild/formatter named-pipe
and documentation Git spawn failures were rerun with authorized execution outside the sandbox.
No pre-existing scientific defect was fixed. No provider data/model or credential was used in CI.
Frontend tests were not rerun locally because frontend is untouched; repository CI remains unchanged.
Branch publication does not claim GitHub CI success (workflow triggers main/pull_request, not this
branch push). GitHub review uses these exact local verification results and the published diff.

## Implementation and temporal contract

`IMarketObservationSource` returns bounded untrusted values, not a knowledge timestamp.
The provider-neutral `MarketObservationReceipt` reuses canonical instrument/effective-dated
provider mapping, MIC, currency, raw decimal prices, timeframe and entitlement references.
Scope is a **one-minute raw OHLCV snapshot**, not a finalized minute bar or a trade tick.
Event time denotes interval opening; absence of an exact last-trade time is not filled in.
Provider publication time is nullable and explicitly unknown when the source does not establish it.
Known publication must satisfy event <= publication <= acquired. Finance samples acquisition only
after the response completes, and ingestion inside its write transaction; acquisition clock regression
and event > acquisition fail closed. All times are UTC. Knowledge time is ingestion, never event,
provider datetime, HTTP Date, or a timestamp supplied by a future caller/model.

The host clock/TimeProvider is trusted infrastructure, not a request field. This is a local receipt
contract, not external clock attestation. A provider's inaccurate old event time cannot grant earlier
knowledge. A bad forward host clock is not automatically repaired; historical replay never uses now
to re-date evidence. Host time synchronization remains an operational prerequisite.

Finance migration96 adds only `market_observation_receipts`, indexes and a singleton durable clock
watermark in the existing database. Existing v1/finite learning readers explicitly allow96 in their
schema checks; their contracts, grants, state validation and science are unchanged. No old row or ID
is converted. Migration rollback, reopen, retained spent v1 authority and existing finite regressions
are tested. No second initialization owner, database, result store or engine.

Both acquisition and knowledge projection use SQLite immediate transactions. A successful projection
seals cutoff T (T <= trusted now), and each new receipt must have ingestion strictly greater than the
persisted watermark (max of prior ingestion and sealed cutoffs). Clock regression/same-tick new writes
fail closed without retry. This provides cross-connection ordering: whichever transaction commits first
determines whether the receipt belongs to that cutoff. Querying older cutoffs remains deterministic.
No query can reserve future knowledge. A cutoff projection is bounded to1000 receipts; overflow fails
closed, with no silent truncation/pagination. A logical revision chain also has a1000-receipt bound.

Exact duplicate identity includes normalized values, interval, source scope and payload checksum;
it returns the original receipt/time even after reopen. Different raw payload bytes are a different
provenance commitment, even if ignored metadata or whitespace changed. Conflicts cannot overwrite:
an explicit exact current predecessor ID is required to append a correction/revision. Normal evolving
snapshots of the same interval therefore need explicit predecessor lineage too; there is no automatic
correction decision. Original and revision remain visible at their respective ingestion cutoffs.
A duplicate with different mapping/instrument/adapter/policy provenance is rejected rather than silently replacing the original.
The projection returns the complete bounded receipt chain; it does not choose a scientifically
preferred revision. Readers verify checksum, indexed identity/time, canonical content, mapping,
origin, temporal validity and predecessor chain; missing watermark/unsupported schema fails closed.
Checksums detect inconsistent records, not malicious database-administrator rewriting of every hash.

Canonical identity uses SHA-256 of ordered JSON string arrays with invariant UTC/decimal formatting;
receipt checksum binds instrument/mapping provenance, policy reference/evidence, source origin,
content, local times and predecessor. Private response bytes are hashed, not stored or logged.
Mapping/policy evidence uses bounded opaque references. Fixture origins remain explicitly distinct
from acquired provider snapshots. Historical import origin is rejected, with no import/backfill API.
A legitimately acquired old event is knowable only at its actual new local receipt time.

## Twelve Data official-source review — 2026-10-02

- [API documentation](https://twelvedata.com/docs#quote): `/quote` supports explicit interval,
  MIC, UTC for intraday, and the documented apikey authorization header. OHLCV refers to that interval;
  `timestamp`/`datetime` denote opening and `last_quote_at` the latest minute. Source HTML was
  inspected locally because the web reader could not fetch the large documentation page.
- [Timestamp clarification](https://twelvedata.com/news/feb-2025-updates): quote timestamp is
  interval opening, not provider publication or BigBrain ingestion.
- [Adjustment documentation](https://support.twelvedata.com/en/articles/5179064-are-the-prices-adjusted):
  intraday prices are unadjusted; daily/weekly/monthly data have different split semantics.
- [Individual pricing](https://twelvedata.com/pricing) currently describes free Basic US equity/ETF
  and internal non-display coverage. [Usage guidance](https://support.twelvedata.com/en/articles/5332349-commercial-and-personal-usage)
  distinguishes internal/personal access from redistribution and exchange-specific requirements.
- [Terms](https://twelvedata.com/terms), particularly2,3,12.5,16: access/storage is conditional on
  tier/source rights and retention/deletion obligations. These facts do **not** prove unrestricted
  durable retention or grant an account, exchange entitlement, strategy-training or external AI rights.
  Historical ADR0021/0022 evidence is preserved; no entitlement policy is silently reinterpreted.

The internal adapter requests only `/quote`,1min,UTC,regular-session,non-EOD,JSON,one mapped symbol
with explicit US MIC (XNAS/XNYS/ARCX) and USD. It requires matching symbol/exchange/MIC/currency,
UTC datetime and Unix minute opening, and matching `last_quote_at`; missing/ambiguous/stale mixed
interval metadata fails closed. No inference from response existence to symbol/strategy eligibility.
Only trusted effective-dated Finance mappings select instruments; broader instrument-master,
corporate-action and delisting treatment remains future work.

Endpoint is fixed HTTPS, redirects/proxies/cookies disabled, no arbitrary URL, one request/no retry.
Default disabled. Configuration follows existing `IConfiguration` section binding at
`Finance:TwelveDataObservation` (`Enabled`, `ApiKey`, `TimeoutSeconds`); a future credential must come
from the accepted external secret/environment mechanism, never Git/chat/logging. No configuration
or credential was added and no runtime registration/endpoint/scheduler exists. Header credential is
never part of URL or provenance. Timeout defaults15s, permitted1–30s; headers <=16KiB, body <=64KiB
before parsing/allocation, JSON depth8, duplicate keys/array envelopes rejected. Unused bounded
provider fields are ignored rather than persisted. Errors exclude response body and inner transport
exceptions. Cancellation propagates; transport failure creates no trusted receipt.

Twelve Data transport is fully fixture-tested, not live-verified. A fixture transport always labels
its output DeterministicFixture. **Live ingestion remains blocked** until explicit compatible
per-use/persistence/retention entitlement is supplied. This bounded store supports only zero-cost,
affirmatively permitted long-term retention with no deletion requirement. Current general Twelve Data
terms do not establish that grant. Subscription-limited retention/deletion and approved backup handling
must be resolved before live activation; no paid subscription or new provider is a workaround.
Existing provider-classified backup exports are not expanded to include these new receipts.

## Security and scientific invariants

Finance owns receipt validation, rights, times, immutable persistence and cutoff projection. No
projection is sent to Brain/Qwen; no admission, grammar, prompt, model/runtime, scientific calculation,
result store, risk policy or grants change. Ledger/schema allowlist compatibility is the only change
to existing learning code. Tests use temporary Finance databases and deterministic transport doubles.
No real provider call, credential acquisition, inference, engine integration, strategy eligibility,
PAPER/LIVE/AUTO, broker/order/capital, scheduler, deployment or BB-132E. No profitability claim.
The next product boundary needs separate review: legitimate live rights/retention and prospective
consumption of this substrate. D does not implement strategy freezing or shadow evaluation.

## Changes

Exactly20 intended files; only the listed compatibility/version assertions change existing learning
code/tests. New tests cover the observation boundary. No Web/Sentinel/runtime/deployment/package change.

- `ARCHITECTURE.md`
- `ROADMAP.md`
- `TESTING.md`
- `docs/BACKLOG.md`
- `docs/STATUS.md`
- `docs/architecture/finance/master-roadmap.md`
- `docs/modules/finance.md`
- `docs/operations/codex-recovery.md`
- `docs/reports/REPORT-CATALOG.md`
- `docs/reports/features/finance/bb-132d-market-observation-foundation-20261002.md`
- `src/BigBrain.Api/Finance/FinanceFiniteResearchSession.cs`
- `src/BigBrain.Api/Finance/FinanceLearningLedger.cs`
- `src/BigBrain.Api/Finance/FinanceSchemaMigrations.cs`
- `src/BigBrain.Api/Finance/FinanceMarketObservations.cs`
- `src/BigBrain.Api/Finance/TwelveDataMarketObservations.cs`
- `src/BigBrain.Modules/Finance/MarketObservationEvidence.cs`
- `tests/BigBrain.Api.Tests/FinanceLearningLedgerTests.cs`
- `tests/BigBrain.Api.Tests/FiniteResearchSessionTests.cs`
- `tests/BigBrain.Api.Tests/FinanceMarketObservationTests.cs`
- `tests/BigBrain.Api.Tests/TwelveDataMarketObservationTests.cs`

## Remaining work

All bounded D implementation/verification work is complete for independent review. Live Twelve Data activation
needs a credential through accepted secret handling and reviewed per-use/retention/deletion/backup
rights; no live call is required for D fixture acceptance. Broader intervals, instrument-master,
projection pagination and prospective evaluation remain future work, not granted by this publication.

## Resumption

Read AGENTS/START-HERE, canonical recovery and this report. Resolve the unique publication subject
`review: implement BB-132D market observation foundation` on branch
`bb-132d/real-market-observation-foundation`; derive exact commit/tree from Git metadata.
Compare against baseline above; unrelated mockups and unpublished ADR0006–0009 are excluded.
After publication STOP for independent architect review; no merge/deployment/BB-132E.
