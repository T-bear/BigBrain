# Finance market-data memory, provenance and learning foundation


## BB-132H versioned observation mapping — review implementation, 2026-10-10

**REVIEW CANDIDATE ONLY.** Bounded mapping assertions extend the existing Finance observation
owner; no production mapping, deployment or live collection is changed. The
[checkpoint report](../../reports/features/finance/bb-132h-versioned-observation-mapping-20261010.md)
records verification and the separately reviewed BB-132G analysis (not accepted implementation).

One configured canonical instrument may supply 1–4 immutable effective-dated snapshots through
`MappingVersions`, instead of the legacy flat mapping fields. The instrument limit remains 4,
independent of the maximum 4 versions per instrument. `InstrumentMappingCatalog` supplies inclusive
source-date resolution and overlap rejection; gaps reject the individual date before network.
Each selected version retains its **complete** `CanonicalInstrument` and `ProviderInstrumentMapping`.
The stable ID, Equity/USD, listing MIC/venue, display name and provider symbol must agree across
versions. This first mechanism permits date/provenance transitions, not unreviewed symbol reuse,
renames, venue changes or other material identity changes. IEX remains the feed, not listing MIC.

`finance-observation-mapping-v1` defines the currentness policy: each version has a bounded
`VerificationEvidence` reference, operator-attested UTC `VerifiedAtUtc`, and explicit exclusive
`RevalidateByUtc`, at most seven days after verification. Verification must not be future/default;
use at/after the deadline rejects. These are trusted operator assertions, not automatic issuer
checks or a provider warranty. Finance independently records the first adoption time from its
trusted clock. Source-day validity is separate from both assertion times and market knowledge.
A null effective end means no known termination **at verification**, never perpetual permission:
the assertion deadline still applies, even when revisiting historical source days.

Versions are frozen at process configuration, sorted by effective start, and uniquely selected
before provider access. Currentness is rechecked after spacing and at acquisition start/commit;
expiry during a request refuses persistence. Renewing verification creates a new immutable manifest
with unchanged snapshots and a new verification reference/time/deadline. Adoption checks every
previously adopted snapshot remains present exactly; removal, shortening or replacement is refused.
An adopted open-ended snapshot cannot be silently closed to admit a successor. Known termination
or a material change requires stopping affected collection and a separately reviewed transition;
this checkpoint does not automate corporate actions. Bounds exhaustion also fails closed.

Additive Finance schema98 contains `observation_mapping_manifests` and
`observation_mapping_receipts` under the same SQLite owner. Persistence is needed because configuration
alone would lose assertion evidence/recording time after restart, and the unchanged receipt envelope
has no currentness fields. Manifest identity hashes canonical serialized snapshots/assertions;
a separate checksum binds that identity and trusted first-recorded time. At most 1000 retained
manifests are read/adopted; no automatic deletion or history compaction. Existing 1000-receipt limits
remain. Clock regression against either market watermark or recorded manifests blocks acquisition.
An assertion may remain after a failed/cancelled provider request; it is not market evidence and
does not advance the market knowledge watermark. Original receipt JSON and hashes are never migrated.

Before network, Finance validates retained receipts and exact configured historical snapshots.
Changed metadata cannot become a value revision. A newly persisted versioned receipt and its manifest
binding commit in one immediate transaction. Duplicate returns the exact original receipt/time and
retains its original binding; old receipts are not retrofitted. Reopen validates bindings at original
ingestion time, so later assertion expiry/reverification does not rewrite or invalidate historical
market knowledge. Expired manifests remain audit evidence, not current permission. Provenance equality
inside `ReobserveDailyAsync` is unchanged. Legacy acquisition cannot bypass adoption by dropping back
to flat configuration, including a race where adoption occurs during an in-flight legacy request.

Legacy single-entry configurations remain supported without fabricated verification dates or grants;
unadopted legacy operation retains its existing explicit owner-review boundary. It gains no versioned
currentness claim. Historical metadata conflicts now fail before network in runtime/F1, rather than
only after response. Once a versioned manifest is adopted for an ID, legacy downgrade is refused.

Cadence, lookback, single-flight, serial work/spacing, recovery, transport limits and default-disabled
state are unchanged. Versions do not multiply provider requests: one instrument/day has at most one
selected snapshot. Cancellation cannot publish a partial receipt/binding. No scientific grant changes;
old ledger readers only recognize the additive schema version.

**SHADOW v1 remains fail-closed for mixed mapping provenance.** No equality checks, frozen identities,
scientific projections or strategy semantics are relaxed. Earlier sealed E remains replayable;
new mixed-history freezes and later evaluations reject. Collection continuity is not scientific
cross-version continuity. Operational adoption/current mapping verification and any cross-version
scientific contract require separate owner/architect decisions. No automatic SHADOW is introduced.
Finance remains **RESEARCH / 0 SEK / NONE**.


## BB-132F1 controlled activation gate — review implementation, 2026-10-04

A finite maintenance entry can acquire one reviewed AAPL/MSFT daily observation while the
unattended runtime remains disabled. It reuses the same adapter, receipt owner, validation and
revision/knowledge semantics; no scientific or execution authority is added. Compose forwards
external settings with no default universe; an immutable build archive supplies assembly/OCI
revision identity. [Operator/configuration/revision contract](../../operations/runbooks/finance-controlled-acquisition.md)
and [exact verification/publication evidence](../../reports/features/finance/bb-132f1-controlled-activation-gate-20261004.md).
No deployment, restart, real request or credential configuration is part of this candidate.

## BB-132F observation runtime — review implementation, 2026-10-03

The API hosts one `FinanceObservationWorker` using the existing `BackgroundService` and
`SystemRecoveryCoordinator` patterns. It waits for recovery/clock permission and drives only
`FinanceObservationRuntime` → existing Alpaca daily adapter → existing Finance receipt owner.
It does **not** call the older EODHD cadence (which also evaluates science), daily-shadow evaluator,
research scheduler, model or trading functions. No new database, schema, cursor, generic scheduler,
endpoint or provider is introduced. [Checkpoint evidence](../../reports/features/finance/bb-132f-live-market-observation-runtime-20261003.md).

### Configuration and activation boundary

Nothing is enabled by this publication. Later live activation requires separate owner authorization,
reviewed instrument mappings/current data-use policy and external credentials. No live call is part
of automated acceptance. Configuration is read/frozen at process startup; disabling requires setting
`Finance:ObservationRuntime:Enabled=false` and a separately authorized restart. Graceful shutdown
cancels the active request/delay. No hot-control endpoint is provided.

| Configuration | Contract |
| --- | --- |
| `Finance:ObservationRuntime:Enabled` | Default false; independent of EODHD/research settings |
| `CadenceMinutes` in that section | Default360, allowed60–1440; delay after completion and on startup |
| `LookbackDays` | Default3, allowed1–7 prior New York calendar days; oldest first |
| `OwnerAcceptanceVersion` | Must explicitly equal `bb132e-owner-data-use-risk-v1` |
| `PolicyRecordedAtUtc` | Nondefault UTC owner-policy recording instant, not future; never used as market knowledge time |
| `AffectedUseEnabled` | Default false; explicit current permission required |
| `CurrentDeletion` | Default Unknown; known deletion obligations block affected collection under unchanged E policy |
| `Instruments` | Explicit1–4 unique instruments; empty by default; no supplied production universe |
| `Finance:AlpacaDailyObservation:Enabled` | Separate existing adapter gate, default false |
| Adapter `ApiKey` / `ApiSecret` | Existing external configuration/secret mechanism only; never examples with actual values |
| Adapter `TimeoutSeconds` | Existing default15, allowed1–30 |

Each allowlist entry supplies `InstrumentId`, `DisplayName`, `ProviderSymbol`, listing `Mic`,
`VenueCode`, `VenueName`, `ValidFrom`, optional `ValidTo`, and `MappingEvidence`. These configuration
fields construct the **existing** `CanonicalInstrument`/`ProviderInstrumentMapping`; they are not
another symbol master. Owner/operator must review actual identity, venue and effective mapping.
The narrow runtime fixes Equity/USD/Alpaca/IEX historical1Day/raw; listing MIC must be XNAS/XNYS/ARCX.
IEX is the **feed**, not the listing venue. Duplicate IDs/symbols, missing/invalid mappings, invalid
limits, missing/malformed credentials or revoked policy fail closed. Secret environment keys use
ASP.NET's existing double-underscore mapping, e.g. `Finance__AlpacaDailyObservation__ApiKey` and
`Finance__AlpacaDailyObservation__ApiSecret`; never put values into chat, repository or diagnostic logs.
No endpoint URL, paid feed, alternative provider, proxy/redirect or runtime tool is configurable here.

### Cadence, evidence and failures

One singleton cycle is active at most; overlapping requests return its snapshot immediately with
no queue. At most4×7=28 slots per cycle; the accepted US calendar skips non-session dates, while
only the unchanged completed-prior-New-York-day/native-provider contract grants daily eligibility.
DST/date conversion reuses `DailyMarketEvidence`. Five seconds separate request attempts, all serial;
there is no retry in a cycle. Ordinary next-cycle reacquisition is bounded by the cadence. Startup
also waits a complete cadence, preventing restart-triggered immediate request bursts. Long outages
are **not** backfilled outside the configured window; missing evidence is never fabricated.

The actual adapter still enforces fixed HTTPS authority, no proxy/cookies/redirects,64KiB bounded
receive and cancellation/timeout. Its HTTP/format/network failures do not create receipts and do not
prevent a separate eligible instrument from being attempted. Clock regression blocks the runtime
until reviewed restart; SQLite/integrity/policy failures stop the cycle. Recovery permission and the
persisted observation watermark are checked before collection. Existing commit checks remain final
scientific authority, including same-tick/sealed-cutoff refusal. No invented timestamp/tick increment.

Finance selects revisions under the same existing immediate SQLite transaction: identical current
content returns the exact existing receipt/time; changed content records a new revision of the latest
receipt. Reversion to an older value is still a new revision, never a reuse/backdate. Mapping/policy/
adapter provenance conflicts fail closed. Explicit predecessor callers retain their existing behavior.
No schema change is needed. A crash before commit rolls back; uncertain post-commit completion is
not retried immediately and restart resolves through immutable evidence. A failed status never means
permission to delete a possibly committed receipt. Earlier sealed knowledge projections remain exact.

`Snapshot` and the existing `HealthCheckService` entry `finance-observation` expose only bounded,
immutable operational data: enabled/state, cycle start/completion, next check, considered source days,
attempted instrument/day, New/Duplicate/Revision receipt IDs and finite failure categories. Overall
existing health endpoints see its health; no new detailed public endpoint is added. Cycle logs carry
state only, never exception text/object or payload/credentials. Operational snapshots reset on restart;
receipts and their original knowledge/provenance do not. Snapshots are not scientific evidence.

This is a single-API-host runtime; cross-host distributed scheduling is not introduced. SQLite still
serializes evidence writes from independent owners. Existing bounded projection/lineage capacities
(including the1000-receipt knowledge projection) remain fail-closed limitations. Operator monitoring,
backup retention, eventual capacity work and explicit live activation remain separate responsibilities.
The model, grant counters, science/risk engines, shadow results and execution authority are untouched.
Finance stays **RESEARCH / 0 SEK / NONE**.

## BB-132E daily prospective evidence — review implementation, 2026-10-03

Status: [ACCEPTED / MERGED / CI VERIFIED](../../reports/features/finance/bb-132e-prospective-shadow-compatibility-20261002.md#accepted-publication--2026-10-03).
[Owner-accepted data-use/risk decision and checkpoint evidence](../../reports/features/finance/bb-132e-prospective-shadow-compatibility-20261002.md#owner-accepted-daily-shadow-implementation--2026-10-03).
The owner decision supersedes the earlier lifecycle implementation stop **only for this private
Alpaca research/evidence scope**. Contractual termination/deletion uncertainty remains. It is not
an Alpaca guarantee, broker permission or automatic live-provider activation.

### Daily source and eligibility

`IMarketObservationSource` and the existing `market_observation_receipts` table now support the
additive `finance-market-observation-daily-v1` envelope. Existing v1 minute receipts retain their
identities/checksums and Snapshot meaning. No minute aggregation, separate Alpaca database or
historical-import relabelling exists. `DailyObservationSemantics` binds eligibility/source version,
New York source date/timezone and effective-symbol policy; the existing receipt binds instrument,
listing MIC/currency/mapping, provider/feed, raw OHLCV, response commitment, acquisition, ingestion,
rights evidence and predecessor. IEX is the feed, **not** the listing venue. Fixtures remain labelled.

The narrow adapter requests one explicit symbol/day from Alpaca historical stock bars with
`timeframe=1Day`, `feed=iex`, `adjustment=raw`, `asof=-`, explicit New York day boundaries and limit2.
Exactly one matching bar and no continuation token are required; absence/ambiguity/incomplete
pagination fail closed rather than invent a daily value. No implicit rename mapping or SIP fallback.
Transport is explicit-construction only, disabled by default, fixed HTTPS authority, no redirects/
proxy/cookies/retry,15-second default timeout (typed1–30),64KiB receive limit before deserialization.
Keys use the existing configuration/secret pattern under `Finance:AlpacaDailyObservation`; no key
or enabled configuration was added. There is no runtime registration, endpoint or schedule.

Eligibility `alpaca-completed-ny-day-v1` requires a **direct historical provider daily observation**
whose New York date is strictly before the trusted request-start date. Its timestamp must be the
New York midnight boundary of that exact source date. Current-day bars are rejected even after
regular market close. This is a completed **source-day observation as returned**, not a guarantee
of final revision, all-market completeness or regular-session-only volume. DST boundaries use the
IANA New York timezone. Provider publication stays unknown; trusted Finance ingestion is knowledge.
Elapsed time alone cannot promote a minute snapshot; the native historical daily source contract
is also mandatory. [Official semantics inspected](../../reports/features/finance/bb-132e-prospective-shadow-compatibility-20261002.md#official-source-characterization).

Exact duplicate evidence retains its original receipt/time. A changed payload/value requires the
exact current predecessor, producing a new immutable revision at a strictly later trusted knowledge
time. Same-tick/regressed writes fail closed. Point-in-time selection first limits receipts by sealed
cutoff, then selects the last recorded revision per logical day **within that cutoff**. No refetch
or present-day latest revision participates in old replay. All-revision projection cap remains1000.

### Owner policy and future obligations

`AlpacaDailyOwnerDecision` records version `bb132e-owner-data-use-risk-v1`, evidence class
`OwnerAcceptedPersonalResearch` and explicit project risk acceptance. Contractual deletion and
post-subscription retention remain Unknown; they are not falsely labelled provider-granted.
This scoped daily policy is separate from unchanged minute-source and historical live readiness
gates. Trusted callers must supply the current policy for acquisition, daily knowledge queries,
freeze and evaluation, including replay. Known deletion obligations, expiry, denied use or revoked
permission fail closed before continued affected use. Policy does not automatically discover future
contract changes: the owner/operator must update the authoritative policy when a concrete applicable
requirement becomes known and obtain reconciliation before further use. No automatic purge exists.

### Freeze, measurement and persistence

Internal `FreezeDailyShadow` takes a persisted native research run ID, effective source mapping,
current policy and trusted TimeProvider; there is no caller freeze timestamp or supplied signal.
The persisted research checksum and payload commitment are verified, and its recorded creation
must be <= freeze. The bounded first supported strategy is existing momentum/v1 with existing
core daily feature periods5/10/20. Other strategies/parameters fail closed; no vocabulary is added.
The supporting native backtest is **identity evidence**, not prospective performance evidence.
No backtest/robustness engine or research grant is invoked by freeze/evaluation.

Known eligible daily receipts generate features through the existing deterministic daily feature
engine and intent through `MomentumResearchStrategy`. Warmup/unavailable features fail closed.
The existing US session calendar guards missing-session gaps; its known calendar scope remains
an eligibility limitation, not evidence of whole-market coverage. The shared strategy context's
portfolio slots are inert zeros; there are no positions/cash state or execution semantics.

One immutable candidate per research run/instrument/provider/dataset binds trusted freeze and
knowledge cutoff, research ID/checksum, source/mapping/origin, feature/projection commitments,
exact receipt IDs, reference close/session, strategy/period/signal and existing
`next-eligible-source-session-close-v1` horizon. Duplicate freeze returns the original; it cannot
refresh an unfavorable hypothesis after new knowledge. Conflicting mapping fails closed.

`EvaluateDailyShadow` requires explicit UTC E <= trusted now and E >= freeze. Only daily receipts
with knowledge strictly after freeze and <= E, for a source session after the frozen reference,
are outcome-eligible. The earliest eligible source day supplies the observed close; revisions are
selected only inside E. Reference-session corrections never rewrite the frozen reference. An older
market date acquired after freeze remains later knowledge; it is not claimed to have occurred later
in wall-clock market time. Missing earlier source days are not fabricated; the horizon explicitly
means next **eligible source** session, not guaranteed next exchange session.

The bounded result records OBSERVED or INSUFFICIENT_DATA, exact candidate/source/projection/cutoff
identities, observed close/reference-close minus1 and directional correspondence to the frozen
intent. This reuses the existing shadow horizon/measurement semantics, not simulated trading.
Raw price movement is not net return, executable profitability, corporate-action-neutral return or
risk approval. IEX coverage and corporate actions limit interpretation. No costs/fills/positions/
orders/cash/annualization/promotion are introduced.

Additive migration97 adds only candidate/result tables under the same Finance SQLite owner;
existing receipts and science stores are reused. SQLite immediate transactions serialize freeze,
cutoff sealing and result publication. Candidate and per-candidate/per-cutoff result keys are unique;
crashes roll back, duplicates validate immutable contents, corruption/missing dependency fails closed.
Later receipts cannot enter a sealed boundary. Reopen reproduces the canonical projection/result.
Old v1/finite research readers merely recognize schema97; spent grants and history are unchanged.

Finance RESEARCH / 0 SEK / NONE. No live call, model, research-grant renewal, trading, broker,
scheduler, public endpoint, deployment or next checkpoint is part of this capability.

## Current Alpaca evidence — 2026-09-06

[Owner-supplied written support evidence](../../reports/features/finance/finance-alpaca-owner-support-evidence-20260906.md)
answers the six submitted private storage/research uses; those questions are no longer
outstanding. Historical SIP is distinct from BB-128A real-time IEX. Activation is still
blocked pending current terms/plan/feed revalidation, unaddressed termination/product
scopes, reviewed policy/technical mapping and separate owner approval. Runtime policy
is unchanged. Older inquiry/qualification/next-step wording below is dated history and
is superseded on these specific answered questions; it must not trigger a duplicate inquiry.


BB-088 runs the BB-087 knowledge-time contract automatically through a bounded cadence. Internal
30-minute state checks do not imply provider access; provider calls are weekday-only after 22:00 UTC
and existing EODHD daily/request/retry gates remain authoritative. Cadence operational metadata
stores timestamps/counts only. It does not copy provider bars or loosen the EODHD deletion scope.

Prospective BB-087 evidence references canonical EODHD market and feature revisions. `observationKnowledgeUtc` records acquisition knowability and `knowledgeCutoffUtc` defines the decision boundary. Identity pins instrument/session, strategy/version, parameter fingerprint and horizon. Later market revisions and outcomes cannot rewrite the original row.

BB-084 extends the ingress boundary with `ExternalDatasetCandidate → QuarantineArtifact →
ValidationEvidence → PromotionDecision → CanonicalDatasetRevision`. Only exact all-pass
`dataset-promotion-v1` evidence may publish. Provider source revisions are never silently
stitched; feature construction selects EODHD or explicit exact revision IDs.

Status: provider-neutral entitlement/provenance code implemented and verified; no external
data, adapter, persistence or runtime is implemented.
Governing decisions: Accepted ADR 0021 for market-data ownership/retention and Proposed
ADR 0020 for evidence and strategy governance.

## Principles

### Collect once, reuse when permitted

Finance should not repeatedly acquire or discard useful history when the exact entitlement
permits local retention. A licensed immutable dataset may be reused for analysis,
backtesting, walk-forward validation, PAPER evaluation, risk research, governed model
training and signal/decision/outcome comparison. Technical possession is never permission:
every use is checked against the dataset's current policy evidence, and unknown rights
fail closed.

### Free first and cost-aware

For external services and data, BigBrain normally evaluates: legally usable free sources,
self-hosted/open-source components, the cheapest sufficient paid product, then more costly
commercial products only for an evidenced need. Cost never outranks security, data
integrity, license compliance or capital/risk controls. Scraping outside terms, unreliable
live inputs and bypassing entitlements are prohibited. Provider-neutral domain contracts
allow a source to be replaced without rewriting strategy, risk or execution logic.

Current product constraint: external Finance market-data spend is exactly **0 SEK**. A paid
source cannot be selected as the first operational source until the product owner explicitly
changes that budget, even when its entitlement is otherwise clear.

Free candidates such as Alpaca Basic/free IEX, Stooq or limited developer tiers remain candidates only. Free
access does not establish storage, corporate-action, derived-use or cancellation rights;
BB-071-quality evidence is required before their data can enter durable storage.

Twelve Data's 2026-08-11 human response clears the submitted personal use on a qualifying
paid Personal plan, including local and post-termination raw/derived/audit retention. It
does not clear Basic/free, redistribution or materially different use. Under the cost order
free → local/open-source → existing BigBrain infrastructure → paid after verified need,
Twelve Data remains a paid fallback while Alpaca Basic/free IEX entitlement is investigated.
The 2026-08-11 BB-075 sweep found no free source with complete exact rights; therefore no
production payload may enter this memory model and the provisional persistence direction
remains unactivated.

BB-076 introduces capability-scoped owner-accepted personal-research evidence for legitimate
0-SEK sources without an identified prohibition. It does not weaken provenance, retention
or technical-access gates. Stooq's bounded daily-history use met the residual evidence class,
but its CSV surface returned a JavaScript verification challenge; no payload entered memory.

BB-077 implements the first concrete production store for the credential-bound EODHD path:
immutable SHA-256-addressed JSON payloads plus a transactional SQLite WAL catalog. Current
rows retain canonical/provider identity, raw OHLC, adjusted close, split-adjusted volume,
session date, acquisition knowledge time, policy and revision. Repeated identical ingestion
is idempotent; changed source content produces another immutable revision, and the read model
selects latest knowledge without erasing old evidence.

BB-078 runtime evidence confirms this store with 2,008 real observations, eight raw payloads
and eight revisions for the complete research watchlist over 2025-08-11–2026-08-10. All
revisions reproduced identical checksums on repeated replay. API/Web recreation preserved
the catalog and payload references, and the acquisition journal prevented a second same-day
download; external request count remained eight.

The EODHD scope is subscription-only. Account termination is explicit configuration, not a
network inference. It blocks acquisition/replay immediately and creates a one-month deletion
deadline. Preview and exact confirmation cover payloads, normalized observations, revisions
and indexes; receipts retain counts/fingerprint but no licensed values. No backup workflow
may include the Finance volume until it can enumerate/delete those copies too.

## Canonical market-data memory

The logical model is independent of transport, provider and physical database:

```text
EntitlementPolicy ── permits/denies ── DatasetRevision
                                      ├── InstrumentMapping
Provider import → RawObservation      ├── CorporateAction
                                      ├── QualityFinding
                                      └── DerivedArtifact → Evidence/Decision references
```

- **Instrument identity:** immutable BigBrain ID; venue/market MIC, currency and
  time-bounded symbol/provider mappings. A ticker alone is never identity.
- **Raw observation:** instrument, venue, timeframe, session/open/close timestamps,
  decimal OHLCV, currency, provider timestamp, retrieval timestamp and raw/adjusted flag.
- **Corporate action:** typed dividend, split or other action with announcement,
  ex/effective/payment timestamps as available, source and revision. Raw prices/actions
  remain separate; adjusted views are deterministic derived artifacts.
- **Dataset revision:** immutable dataset/import ID, parent/superseded revision, provider
  dataset/product, requested scope, adapter/schema version, checksum and completion state.
- **Quality finding:** valid, incomplete, stale, duplicate, gap, conflict, corrected,
  quarantined or rejected, with calendar/version evidence and non-secret reason codes.

Corrections append a revision and supersession link. They never silently overwrite a
dataset referenced by an evidence report. Replay identifies the exact dataset revision,
adjustment algorithm and calendar version.

## Provenance and entitlement envelope

Every persisted raw record, logical dataset and derived artifact inherits an entitlement
envelope containing at least:

- provider and provider dataset/type/product;
- retrieval time and provider/market observation time;
- canonical market/instrument identity and provider mapping;
- policy ID/version, governing terms/evidence reference and review date;
- retention class and allowed-use set;
- `Raw` or `Derived`, plus parent dataset/revision references;
- whether persistence and post-subscription retention are explicitly permitted;
- maximum retention/deletion deadline and deletion scope when known;
- provider correction/version markers and BigBrain import/schema version.

Allowed uses are explicit values, not free text: `LiveDisplay`, `HistoricalAnalysis`,
`Backtest`, `WalkForward`, `PaperTrading`, `StrategyTraining`, `DerivedMetrics` and
`LongTermStorage`. Policy answers one of `Allowed`, `Denied` or `Unknown` for a requested
use at a point in time. `Unknown`, expired evidence, missing policy, ambiguous product or
an unrecognized use always denies persistence/use. An account or successful API response
does not prove entitlement.

Deletion policy must distinguish raw observations, normalized copies, backups, corporate
actions, derived artifacts and audit metadata. If deletion is required, Finance stops new
uses, records a sanitized deletion event and removes all covered copies through a later
owner-approved procedure. It must not claim reproducibility for evidence whose licensed
inputs can no longer be retained. Non-sensitive entitlement/audit metadata may remain only
when the governing terms and policy allow it.

## Raw provider data and BigBrain-derived knowledge

Provider raw data includes responses, canonicalized observations and corporate actions
that can reproduce the provider facts. BigBrain-derived data includes indicators,
features, volatility measures, signals, scores, decisions, backtest results, statistics
and model outputs. Every derived artifact records its algorithm/model/parameter version,
input dataset revisions, creation time and output checksum.

“Derived” is lineage, not a license exemption. A provider policy may deny creating,
retaining, reverse-engineerable or post-subscription derived artifacts. The entitlement
evaluator checks derived creation and each later use independently.

### Owner approval versus external rights

Owner-controlled intake may record `APPROVED_BY_OWNER` as an auditable private-research policy
decision with its own evidence reference. It never means that Google or another upstream provider
verified storage, reuse or backtesting rights. External license/provenance remains independently
`Pass / Fail / Unknown`, and `Unknown` remains fail-closed. The same separation applies to an
owner-declared raw/adjusted basis, venue and symbol identity.

BB-127 adds a purpose-specific research capability layer for immutable owner-provided revisions.
It does not relax the entitlement envelope or canonical promotion. An owner-accepted dataset with
unknown external evidence may be used only for a compatible private-research purpose when artifact
integrity and technical evidence suffice and no known rule explicitly denies that use. Every
result carries the candidate, artifact/workbook and dataset fingerprints, owner evidence version,
external state, purpose decision and unresolved limitations. An explicit external `Fail` remains
blocking. Current-snapshot metadata cannot be represented as historical point-in-time evidence.

## Decision and outcome evidence graph

The append-oriented evidence chain is:

```text
market snapshot → signals → strategy evaluation → risk/policy evaluation → decision
  → order intent / paper order / no trade → execution → outcome → post-trade evaluation
```

Stable correlation/causation IDs connect timestamp, instrument, dataset/snapshot revision,
strategy and parameter version, signal/score, risk state, mode, decision/reason codes,
rejected alternatives, immutable order intent, execution/fill, fees/slippage and outcomes
at named horizons. Outcome records distinguish realized and unrealized results and retain
the valuation dataset/version. NO TRADE and REJECTED remain first-class evidence so later
analysis does not select only executed winners.

A query such as “how did strategy X perform after this signal in comparable conditions?”
must resolve declared feature/regime definitions, input entitlements, dataset versions,
decision population (including rejects/no-trades), cost model and outcome horizon. It may
not silently compare differently defined or no-longer-permitted evidence.

## Controlled learning governance

Finance follows `COLLECT → MEASURE → BACKTEST → VALIDATE → PAPER TRADE → REVIEW → PROMOTE
→ LIVE`. Collection may produce evidence; it cannot mutate an active strategy. Models,
features, parameters and strategies are immutable versioned candidates. Promotion requires
the existing lifecycle gates, Risk Engine constraints and explicit owner approval; rollback
suspends a version while preserving permissible evidence.

Validation must address out-of-sample and walk-forward performance, overfitting,
survivorship/look-ahead bias, data leakage, selection and multiple-testing/data-mining
bias, regime change, transaction costs and slippage. Historical success is not proof of
future profitability, and no automatic retraining or recent-performance promotion may
affect real money.

## Storage architecture direction

Finance owns its storage inside the modular monolith. Other modules use versioned Finance
capabilities and never read its tables/files. Start with repository-native, self-hosted
components and no new service: immutable content-addressed import files where justified,
plus a transactional metadata/catalog store following the established modullocal SQLite
pattern for the first bounded EOD allowlist. Schema migrations are explicit, versioned and
backed up; datasets and journals use stable IDs rather than database row identity.

This is a provisional implementation direction, not authorization to create storage now.
Before BB-045 chooses SQLite/blob layout, measure expected daily EOD volume, replay/query
patterns, backup/restore time, concurrent readers and deletion-policy needs. PostgreSQL,
columnar files or another engine requires measured pressure and an architecture review;
no paid service or new container is justified today.

Backups inherit each record's entitlement and deletion deadline. A backup is not a way to
evade retention. Restore tests must prove schema/data version compatibility, provenance,
checksum integrity and licensed deletion across primary copies and backups.

BB-085 implements this as `finance-provider-backup-v1`: source classification precedes
selection; WIKI public-domain revisions and exact source-only derived rows are serialized in
stable order, while EODHD is inventoried as restricted and excluded from indefinite backup.
Only a COMPLETE manifest published after write/hash/verify is restorable. Restore is staged,
checksum-verified and identity-compared without overwriting healthy canonical memory.

## Implementation gates and next safe slice

BB-079 implements immutable derived feature memory alongside the market catalog. A feature
revision binds all source market revisions, `core-daily-v1` fingerprints and engine version;
values retain source range and causal knowledge time. Rebuild is idempotent and old revisions
are not overwritten. EODHD-dependent feature values/revisions/indexes inherit the active-
account and confirmed deletion lifecycle. See ADR 0023 and the BB-079 report. The next safe
slice is an offline deterministic M3 research harness referencing exact market and feature
revision IDs.

BB-080 completes that first harness. Backtest runs, event journals, simulated fills, equity/drawdown curves, metrics and indexes retain exact source market/feature lineage and the EODHD subscription-only deletion obligation. Generic versioned strategy definitions contain no licensed values and may remain. The engine performs no provider IO.

Provider-neutral work may proceed before BB-071: typed policy/usage/classification models,
an in-memory fail-closed entitlement evaluator, provenance envelopes, fixture-only dataset
revisions and invariant tests. No real provider payload may be persisted.

Implemented 2026-08-10: these types and evaluator now exist in
`src/BigBrain.Modules/Finance/MarketDataEntitlements.cs`. The evaluator binds requests to
exact provider/product scope and returns the source decision plus a stable reason while
effective use remains denied for missing, unknown, denied, mismatched or expired policy.
Tests are synthetic and deterministic. Persistence and external adapters remain absent.

Implemented 2026-08-11: `CanonicalMarketData.cs` adds stable Equity/ETF identity and
inclusive effective-date provider/product/symbol/MIC mappings. Synthetic daily decimal
OHLCV and separate dividend/split actions normalize deterministically while preserving
raw/adjusted basis, dataset revision, policy identity and source/retrieval provenance.
Overlaps, unknown mappings, invalid ranges, negative volume/currency mismatches and invalid
actions fail explicitly. Exact/conflicting duplicate outcomes are stable and non-overwriting.
Without an exchange-calendar implementation, expected closure, unknown missing observation
and provider gap remain separate concepts and are never guessed.

Implemented 2026-08-11: `HistoricalReplay.cs` adds explicit fixture sessions with
Trading/Closed/Unknown state, venue/MIC/timezone/evidence, safe UTC/DST conversion and
stable gap classifications. Replay accepts one immutable revision and a supplied UTC range,
uses historical provider references and source availability timestamps, emits separate
session/quality/observation/dividend/split events and sorts through an explicit deterministic
priority/tie-break contract. It never fills, interpolates, mutates or silently combines
revisions. This is an M2 data primitive, not strategy or portfolio simulation.

Implemented 2026-08-11: `DatasetRevisionAssembly.cs` assembles immutable parent/child
snapshots from ordered members and explicit corrections. Event time remains inside the
canonical fact; member/correction availability and revision creation are separate UTC
knowledge times. Correction availability is inclusive at `as-of`, cannot precede its
original or revision, and relates immutable original/replacement IDs plus reason/evidence.
Old revision snapshots stay addressable; linear acyclic supersession prevents ambiguous
branches and deterministic ordering ignores hash/input order.

Exact provider/product entitlement, cost-first selection and explicit activation approval
remain the external gates for retaining actual provider data. BB-045 remains the ingestion/
storage implementation and must not activate a provider until those gates pass. BB-072 free historical source research completed on
2026-08-11 without authorizing a source: EODHD Free Starter and Twelve Data Basic remain
conditional evidence leads and the recommendation is `DO NOT INGEST YET`. Only exact
written entitlement plus product-owner review may precede an adapter/acceptance slice.
Persistence selection remains separately measurement-gated.

The combined historical/current gate keeps bootstrap and forward observations under one
convergence rule: canonical identity may match, but provider/product, four-clock
availability, raw/adjusted basis, policy and revision lineage remain separate. Overlap is
deduplicated only when evidence agrees; disagreement and corrections append explicitly.
Twelve Data Basic is only a conditional small-US lead. No provider memory may begin until
exact written entitlement and owner approval are recorded.

Implemented fixture-only acquisition preparation now provides the future ingress contract:
exact request/product/range/policy/destination revision, immutable batch/pagination/
completeness/provenance, stable observation IDs, explicit gaps/corrections and an immutable
audit journal. The gate evaluates analysis, backtest, derived metrics, long-term storage and
persistence before invoking an adapter. Accepted synthetic facts reuse canonical
normalization and immutable assembly; content is never silently repaired or overwritten.
No durable provider memory, provider transport or real entitlement was added.

Implemented 2026-08-11: `HistoricalDataPersistence.cs` defines the immutable manifest,
SHA-256 content identity, storage contract and fixture reference implementation. A bounded
benchmark compared append-oriented JSONL and transactional indexed SQLite at 2,520,
126,000 and 1,260,000 synthetic EOD rows. JSONL favored compact sequential initial writes;
SQLite favored exact/range queries, transactions and catalog operations. The provisional
direction is therefore immutable content-addressed payload files plus SQLite manifest,
index, lineage and deletion metadata. Confidence is medium pending backup/restore,
concurrent-reader and deletion-across-backup validation. No ADR or production store is
accepted by this measurement alone.

## Forward/live observation memory

Authorized current observations should accumulate into the same entitlement-aware local
memory without being confused with previously acquired history. Each forward fact preserves
event, provider, received and first-usable knowledge time plus stream/journal identity and
honest realtime/delayed/EOD freshness. Daily permitted batches may publish new immutable
revisions; corrections append and never rewrite what BigBrain actually knew earlier.

This prospective stream is especially valuable because it proves the knowledge boundary at
the time of observation. Shadow predictions and later outcomes are derived artifacts with
lineage and policy, not a loophole around provider deletion. Local reuse reduces calls and
cost only while the exact entitlement permits retention; expiry/deletion must cover raw,
canonical, derived and backup copies while sanitized audit receipts may remain only where
allowed.

## Persistence requirements derived from revision assembly

Future storage must support append-only revision metadata; immutable observation/action/
finding bodies; parent and superseding revision links; original→replacement correction
chains; UTC event-time and availability-time indexes; canonical logical identity; provider,
product, policy, provenance and deletion metadata; deterministic membership/as-of queries;
and checksums/backups that retain entitlement. The first bounded size/range/deletion
measurement is complete; concurrent readers, backup/restore and licensed deletion across
backup copies remain. No production persistence technology is selected or activated here.

BB-074 projects only sanitized revision, coverage, count, persistence, policy and provenance
summaries to Web. Raw payloads, credentials, secret URLs and legal correspondence are not
fields in the read contract. Production remains empty until authorized canonical memory is
published through the normal Finance boundary.
