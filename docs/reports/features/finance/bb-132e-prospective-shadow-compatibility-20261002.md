# BB-132E — Prospective shadow compatibility characterization

Detta är en sanerad GitHub-version. Synthetic fixtures only; no credentials, provider payloads,
private runtime paths or model content.

Current state: [Accepted publication](#accepted-publication--2026-10-03).
The initial characterization and its verification below are retained as reviewed history.

## Metadata

Date: 2026-10-02. **REVIEW CHECKPOINT / INCOMPLETE / NOT A MERGE CANDIDATE.**
Goal: freeze existing research before future knowledge, then evaluate prospectively against
BB-132D receipts without introducing trading or a second scientific engine.
Fetched baseline/main: `8c09fd4dce4147766b51979ecd2cacc21d87eb92`; tree
`69ae54c5b3b2243c71bcbf9755a71383f31e1d8f`. Branch:
`bb-132e/prospective-shadow-evaluation`.
Owner accepted D commit `85163a9a648a65afded90a72f9433f2bc5389c58` is the baseline's parent;
D's dated pre-acceptance report is preserved, not treated as a missing grant for characterization.

Publication subject: `review: characterize BB-132E prospective shadow compatibility`.
Resolve that unique commit on the branch with `git log --format='%H %T %P %s'`; Git metadata
provides the exact review SHA/tree and baseline diff without a self-referential document hash.

## Status

This is the explicitly requested architectural compatibility stop, **not a demonstrated
pre-existing correctness/security/lineage defect**. Accepted components retain their documented
meaning. No production fix, shadow candidate/result schema or evaluation path was implemented.
Passing characterization tests do not satisfy E's product acceptance criteria.

## Characterization and reusable ownership

| Existing boundary | Actual accepted meaning | Consequence for E |
| --- | --- | --- |
| `MarketObservationEvidence.cs` | `MarketObservationReceipt`: Snapshot, Raw, OneMinute; event is interval opening; publication may be unknown; knowledge is trusted ingestion | Receipt provenance/time is reusable; neither elapsed time nor receipt existence attests final bar/session close |
| `FinanceMarketObservations.cs` | Finance SQLite owner, migration96; immutable receipt/revision chain, checksum validation, transactional sealed cutoff/watermark, at most1000 receipts | Reuse acquisition/readers and conservative knowledge boundary; query returns all revisions, not a scientific revision-selection decision |
| `DailyFeatureEngine.cs` | `core-daily-v1`, DateOnly session identity, one observation per instrument/session, daily SMA/momentum lookbacks and gap/warmup rules | Dropping minute time aliases distinct observations; choosing one or aggregating them requires an explicit finality/completeness/session rule |
| `DeterministicBacktesting.cs` | buy-and-hold, SMA and momentum research strategies; `daily-next-session-open-v2` uses daily bars, features, fills, cash and positions | Its simulation results cannot be repackaged as E's non-trading snapshot outcome measurements |
| `RobustnessEvaluation.cs` and `FinanceFiniteResearchSession.cs` | Existing bounded daily evaluator and native result persistence; finite session invokes momentum with frozen bars/features and one evaluation grant | Retain identity/readers/grants; replaying another robustness experiment is not prospective validation |
| `FinanceShadowResearch.cs`, ADR0029 | Existing EODHD current-session prediction/outcome journal with `next-eligible-source-session-close-v1`, causal daily features and source-session lineage | Useful immutable knowledge-time precedent, but not an adapter for D minute snapshots; no source-session close can be inferred from them |
| `LiveObservationLearning.cs` | `SyntheticShadowLearningPipeline` explicitly admits only `TEST-SYNTHETIC-MOMENTUM-NON-TRADING` | Cannot promote the fixture to an accepted research strategy by renaming momentum or mapping receipt fields |
| `StrategyContracts.cs` | `IFinanceStrategy` is a contract, with no concrete implementation found in current src | An interface alone supplies no accepted snapshot calculation/outcome semantics |

Inspected existing Finance backtest/robustness stores and their immutable native identities rather
than creating parallel persistence. ADR0025's chronological evidence and ADR0038's Finance
authority remain applicable. D's live rights/retention gate remains separate and unchanged.
No live provider validation is needed to prove this code-level compatibility boundary.

## Smallest blocking decision

The missing primitive is a scientifically approved relation between **one-minute raw snapshots**
and the **frozen daily hypothesis's prospective outcome**, not a missing database or time field.

Even freezing a previously computed daily signal does not make a later snapshot close the accepted
next-source-session close. Snapshot finality, session coverage, revision selection, horizon and
meaning of the measured outcome must be established before that comparison can be called reuse
of the existing semantics. A return ratio can be computed arithmetically without establishing this
scientific meaning. We do not invent it to fill an attractive result contract.

Possible bounded directions for independent owner/architect decision (neither implemented):

1. Establish a versioned finalized daily observation/eligibility contract compatible with the existing
   daily strategies and source-session horizon, including session completeness, provenance, revision
   and trusted knowledge rules. D snapshots alone do not prove such a final bar.
2. Explicitly approve a snapshot-specific, non-trading measurement contract for frozen hypotheses,
   defining reference price, observation eligibility, horizon, revisions and limitations. This would
   be a new scientific meaning, not an automatic cast into the existing daily engine.

Do not change strategy periods from days to minutes, manufacture a session close, treat latest
revision as preferred truth without policy, fabricate provider publication, or promote synthetic
fixture behavior. No new ADR or implementation decision is made by this report.

## Model-free characterization witnesses

`ProspectiveShadowCompatibilityTests` adds four tests using the accepted D acquisition API and
existing temporary Finance database fixture; it inserts no convenient observation rows directly:

1. Persist/reopen two legitimate same-day minute snapshots. They remain Raw/Snapshot/OneMinute.
   A deliberately lossy **test-only** DateOnly relabelling fails the real daily feature builder
   with `Feature inputs must be unique per instrument/session.` This is a counterexample to a
   naive adapter, not a claim that the daily engine accepts receipt objects or detects every possible
   semantic forgery. Selecting just one snapshot could evade that structural check but would still
   lack finality/session evidence.
2. Persist an explicit revision and reopen both receipts. The original survives; both remain snapshots;
   the earlier cutoff returns only the original. No final-bar promotion follows from correction lineage.
3. Seal hypothetical freeze/cutoff instants using D. Acquire older-event observations later; they are
   known only at ingestion. Adding evidence after the sealed cutoff leaves the reopened serialized
   projection (including its checksum) byte-identical. This proves reusable D knowledge behavior,
   **not an implemented E candidate or shadow-result noninterference guarantee**.
4. Passing the accepted momentum identity to the synthetic snapshot pipeline fails with its explicit
   non-production-only guard. No scientific evaluation or trading simulation is run by these new tests.

## Evidence

This review delta consists solely of
four characterization tests and five Markdown documents. Production code, schema and contracts
are byte-unchanged from baseline. Scope is intentionally smaller than E's final Merge Candidate:
targeted Release test compilation/execution, relevant D/shadow regressions, changed-file format,
documentation and exact scope/secrets checks. Full API/solution builds and Sentinel/frontend suites
are not repeated for this test/document-only architecture stop; there is no changed shared runtime
or migration. Full risk-justified verification remains necessary if implementation is authorized.

Checks on 2026-10-02:

| Command | Result |
| --- | --- |
| `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --configuration Release --no-restore --filter 'FullyQualifiedName~ProspectiveShadowCompatibilityTests\|FullyQualifiedName~FinanceMarketObservationTests\|FullyQualifiedName~FinanceShadowResearchTests\|FullyQualifiedName~FinanceLiveObservationLearningTests'` | PASS54/0 failures/0 skips, including all4 new tests; Release test/dependency compilation passed |
| `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~FinanceDailyFeatureEngineTests\|FullyQualifiedName~FinanceDeterministicBacktestTests\|FullyQualifiedName~FinanceRobustnessEvaluationTests'` | PASS27/0 failures/0 skips |
| `dotnet format tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --verify-no-changes --no-restore --include tests/BigBrain.Api.Tests/ProspectiveShadowCompatibilityTests.cs` | PASS, exit0 |
| `node scripts/verify-documentation.mjs` | PASS,262 Markdown files/91 unique backlog IDs; links/indexes included |
| `git diff --cached --check` | PASS |
| Gitleaks8.28.0 `git . --pre-commit --staged --redact --no-banner` | PASS, no leaks in exact staged content |
| Exact staged inventory versus the six paths below; baseline/unstaged checks | PASS; no production/schema/runtime/old test change or unrelated file |

No GitHub CI result is claimed by this local evidence. No full-repository test result is inferred.

Initial sandbox attempts failed before testing/document verification (MSBuild named-pipe permission
and Node Git spawn EPERM); rerun with authorized execution outside sandbox. Initial test compilation
reported CA1861 for a constant fixture array; moved its checksum to a static readonly fixture field,
then all tests above passed. Initial report verification required the standard section headings;
headings corrected without changing findings. These were development checks, not product failures.

## Changes

- `tests/BigBrain.Api.Tests/ProspectiveShadowCompatibilityTests.cs`: executable characterization.
- `docs/STATUS.md`: current review state and link to this evidence.
- `docs/BACKLOG.md`: blocked product DoD and required decision.
- `docs/operations/codex-recovery.md`: exact continuation/publication handoff.
- `docs/reports/REPORT-CATALOG.md`: report discovery.
- This report: technical findings, tests, limitations and review decision.

AGENTS, architecture, ADRs, roadmap, module contract and TESTING were assessed: no accepted
contract/direction/command changes are needed for this characterization. Evidence and exact commands
belong here rather than being duplicated throughout those documents. Existing D/A/B/C reports remain
unchanged. Pre-existing untracked mockups and unpublished ADR0006–0009 are excluded.

## Remaining work

E's immutable candidate/freeze, science-evidence binding, trusted evaluation cutoff sealing at the
candidate level, scientific calculation, immutable result persistence/reopen and evaluation concurrency
are **NOT IMPLEMENTED**. No claim that the requested sixteen acceptance cases or shadow-result
noninterference have passed. Tests only establish the boundary and the reusable D substrate above.

## Security

Finance remains **RESEARCH / 0 SEK / NONE**. No Qwen/model or provider call, new finite session,
grant/reset/refund, historical experiment, engine/runtime/parser change, real-data entitlement,
PAPER/LIVE/AUTO, broker/orders/positions/capital, scheduler/daemon/public endpoint, paid data,
deployment or BB-132F work. All test databases are disposable fixtures. No prior private scientific
evidence is opened or modified. Ordinary tests remain model-free and credential-free.

## Resumption / next action

STOP after publishing this Review Checkpoint for independent ChatGPT/system-architect review.
Resolve exact SHA/tree from the publication subject, compare against the baseline above and read
the four tests. Owner/architect must approve a bounded semantic direction before production work.
Continue additively on this same branch only after that decision; do not merge this incomplete
checkpoint, redesign science opportunistically or start the next checkpoint.

## Finalized-daily continuation — 2026-10-02

**REVIEW CHECKPOINT / INCOMPLETE / NOT A MERGE CANDIDATE.** Owner/architect independently
reviewed `d058d674f0e2403c1015a44b1176d3bfc0449284` (tree
`109154ad1c43bbd7569909c85d516c21926a2636`) and selected direction1: finalized daily evidence
compatible with existing daily science. Direction2/intraday science is excluded. This resolves
the choice in the initial report; it does not supply the missing source facts described here.
Baseline/main and branch remain those above. The new additive publication subject is
`review: characterize BB-132E daily finality prerequisite`; derive its exact SHA/tree from Git.

### Inspected responsibility and the precise missing fact

`ProviderObservation` and `MarketObservationReceipt` retain identity, one-minute interval opening,
OHLCV snapshot, nullable provider availability, trusted acquisition/ingestion and revision lineage.
They contain no assertion that a minute is complete, that a session is complete, or that a chosen
set covers the provider's complete session. A known provider availability timestamp, if present,
would date availability of the snapshot; it would not assert finality.

`TwelveDataMarketObservations` requests one `/quote` with `interval=1min`, `prepost=false`,
`eod=false`. Its validated `timestamp`/`last_quote_at`/datetime identify the interval opening.
It returns no completion manifest or finality evidence; unused bounded provider fields are not
interpreted as authority. An `is_market_open=false` hint cannot prove which session/minutes/values
are complete. These are findings about the accepted code, **not claims that Twelve Data lacks
other products or currently guarantees some new field**. No network/source-policy change is made.

`UsMarketCalendar` provides a versioned nominal New York schedule, dates/holidays and fixed local
09:30–16:00 bounds. Its interface takes only a date, not a source/MIC completeness assertion;
there is no per-session early-close/halt/exception evidence input. Passing scheduled close is not
provider delivery completion. This is a limitation for the new proof, not a repaired calendar defect.

`HistoricalDataAcquisitionBatch.Completeness` classifies supplied historical acquisition batches.
Its daily request/normalizer consumes already-described daily bars and received-time/provenance;
it does not establish completed minute/session coverage for D's live snapshot receipt set. Neither
that enum nor a daily DTO constructor can retroactively attest a D session. Existing canonical
instrument/mapping and daily feature/strategy/outcome contracts remain reusable after eligibility,
not substitutes for it. No historical bar is imported as prospectively acquired evidence.

**Smallest missing primitive:** an explicitly trusted, versioned **source/session completion
commitment**, acquired and persisted by Finance, binding canonical instrument/MIC, source/product,
session date and actual bounds, and the exact finalized constituent observation revisions whose
coverage/value semantics it attests. A bare `complete=true`, a closed-market hint or a calendar
timeout is insufficient. For the chosen minute-to-daily route this must establish both completed
constituent bars and complete session coverage (including explicit treatment of no-trade/gap/closure
intervals), with compatible raw OHLCV/volume semantics. No such fact exists in the inspected D path.

An explicit source-guaranteed finalized daily bar is a possible *alternative evidence route* for
owner/architect assessment; it must carry its own complete-session meaning and trusted receipt,
and cannot be fabricated from or silently substituted for current snapshots. No new provider,
endpoint, field, price-basis policy, entitlement or live acquisition is authorized/implemented here.

### Minimum finalized-daily contract requirements

This is a characterization of necessary facts, **not an implemented or accepted new protocol**.
The missing completion commitment prevents proving the positive eligibility path. Fail closed
before constructing a daily science input; do not publish a misleading always-ineligible finalizer.

| Requirement | Minimum meaning / proof obligation |
| --- | --- |
| Version, instrument, venue/session | Version derivation/eligibility policy; reuse exact canonical instrument, effective mapping, MIC, currency, provider/product and raw price basis. Bind session date, timezone/calendar version and attested actual bounds; no ticker-only or UTC-date guessing |
| Source granularity | D receipts remain immutable raw one-minute snapshots. Aggregation requires affirmative finalized interval semantics for the selected revisions; changing the label is not evidence |
| Completeness/finality | Bind the source completion commitment to every selected constituent and whole actual session. Missing minute, unknown gap/no-trade classification, missing close/open coverage or unknown source finality is ineligible. No invented values or silence-based completion |
| OHLCV derivation | Only after coverage and compatible interval definitions are proven: first completed interval open, maximum high, minimum low, last completed interval close; volume sum only for proven non-overlapping interval volumes with identical units/session scope (not cumulative snapshots). No implicit carry-forward, aggregation of revisions twice or missing-minute zero fill |
| Revision choice | Choose only exact revisions affirmatively bound by completion evidence and knowable at sealed cutoff. D returns all revisions. Latest receipt alone is not proof of finality. Later corrections create new immutable daily evidence/lineage, never alter an earlier result |
| Trusted daily knowledge | Cannot precede knowledge of any selected receipt or completion/session fact. Finance must validate and persist finalization at trusted local time; an old event/provider date cannot backdate it. Preserve source-availability and derived-persistence times distinctly; never infer an unknown publication time |
| Provenance and canonical identity | Bind policy/version, instrument/session/source scope, sorted constituent receipt IDs/checksums, explicit revision selection, completion evidence identity/checksum/time, normalized values, bounds/price semantics and trusted derived receipt time. Reuse canonical hashing/persistence patterns; no hash of values alone |
| Restart and duplicate | Persist exact inputs/commitment and verified daily identity under existing Finance ownership, not a second store. Reopen validates all dependencies. Duplicate identity reuses original times; missing/conflicting evidence fails closed |
| Cutoff/noninterference | E must be <= trusted now. Every dependency must be knowable by E, including completion. Later receipt/completion/correction must not enter or alter a sealed earlier projection/result; a new daily revision becomes eligible only at its own legitimate knowledge time |
| Downstream compatibility | Only eligible daily evidence may feed existing daily features/frozen supported strategy and next-source-session outcome semantics. No change to periods, research grants, risk, backtest simulation or trading authority follows |

The existing D receipt owner/watermark can support source dependency replay; it currently has
no completion-evidence representation. Exact additive persistence/concurrency design for a new
completion fact is deferred until its trust/source meaning is reviewed. No speculative migration.

### New deterministic witnesses

Two additive tests, leaving the initial four unchanged:

- `FullScheduledMinuteCoverageAndElapsedCloseDoNotEstablishFinality`: ingests a fixture snapshot
  ten seconds into **every** nominal minute (390 for the selected ordinary session) using D's real
  acquisition/persistence API. After scheduled close and reopen, all390 remain snapshots acquired
  inside their intervals. Silence and an exact duplicate do not refresh the receipt. A permitted
  post-cutoff revision of the last interval preserves the original and byte-identical earlier
  projection. This witnesses that interval-key coverage plus elapsed close is weaker than completed
  interval coverage. A later correction alone is **not** claimed to disprove finality: finalized
  evidence can also be corrected, but needs an affirmative source commitment at each revision.
- `MarketClosedHintAndLaterAcquisitionStillProduceOnlyMinuteSnapshot`: runs the existing Twelve
  Data adapter with deterministic HTTP transport and a synthetic closed-market hint, received on
  the next day. The persisted/reopened result is still a one-minute snapshot, publication unknown,
  knowledge equal to actual trusted ingestion. No network or credential is used.

These tests exercise actual boundaries and demonstrate missing semantics; they do not implement
or claim passing E's candidate freeze, finalized daily identity/result or shadow acceptance cases.

### Continuation verification and scope

Delta: the two test files above plus the same five
canonical documents (report/catalog/status/backlog/recovery). Production, calendar, adapter,
daily engine, previous four tests, schema, runtime and grants remain unchanged.
The initial27 daily-feature/backtest/robustness regression results remain applicable: those exact
sources/tests and scientific assumptions did not change. No reason to rerun full suites solely
for this additive characterization. Focused D/adapter/characterization tests, Release test build,
changed-file format, docs/diff/secrets checks verify this delta. Full API/migration/Sentinel/frontend
and real-provider/model tests are not run; no corresponding production surface changed.

| Exact command/check (2026-10-02) | Result |
| --- | --- |
| `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --configuration Release --no-restore --filter 'FullyQualifiedName~ProspectiveShadowCompatibilityTests\|FullyQualifiedName~FinanceMarketObservationTests\|FullyQualifiedName~TwelveDataMarketObservationTests'` | PASS61,0 failures/0 skips; Release test/dependency compilation passed; both new witnesses included |
| `dotnet format tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --verify-no-changes --no-restore --include tests/BigBrain.Api.Tests/ProspectiveShadowCompatibilityTests.cs tests/BigBrain.Api.Tests/TwelveDataMarketObservationTests.cs` | PASS, exit0/no changes |
| `node scripts/verify-documentation.mjs` | PASS,262 Markdown/91 unique backlog IDs |
| Production and daily-feature/backtest/robustness test diff against reviewed parent | Empty; prior27 PASS explicitly reused, not rerun |
| `git diff --cached --check`; exact seven-file staged inventory and unchanged reviewed parent | PASS; unrelated files excluded, no unstaged tracked delta |
| Gitleaks8.28.0 `git . --pre-commit --staged --redact --no-banner` | PASS, no leaks |

No GitHub CI or full-repository green result is inferred. Prior scientific/model journals were not
opened or mutated. No source compilation fix or formatting write was needed in this continuation.

### Remaining decision and stop

Direction1 is retained. Owner/architect must now decide the bounded trustworthy source/session
completion fact and acquisition contract, with sufficient evidence for finalized constituents and
session coverage. **No permission is requested again for the already selected daily direction.**
No daily finalizer, candidate freeze, prospective evaluator/result or schema is implemented.
This is the prompt's explicit missing-primitive stop, not a demonstrated pre-existing defect or a
Merge Candidate. Preserve prior review history and continue the same branch only after that decision.

Finance **RESEARCH / 0 SEK / NONE**; no Qwen, live provider, grant reset/refund, PAPER/LIVE/AUTO,
broker/orders/positions/capital, scheduler, automatic promotion, deployment or BB-132F.
STOP after publication for independent architect review; no merge.


## Alpaca daily evidence and rights gate — 2026-10-03

**REVIEW CHECKPOINT / INCOMPLETE / NOT A MERGE CANDIDATE.** This continuation stops at
an unresolved provider-rights prerequisite, not a demonstrated defect in accepted Finance code.
Owner/architect selected historical Alpaca US equity `1Day` observations. Do not aggregate D
minute snapshots or use Twelve Data as fallback. Earlier completion-fact investigations remain
historical evidence; this decision replaces their proposed minute-aggregation acquisition direction.

### Exact scope and publication identity

Accepted main remains `8c09fd4dce4147766b51979ecd2cacc21d87eb92`, tree
`69ae54c5b3b2243c71bcbf9755a71383f31e1d8f`. Same branch
`bb-132e/prospective-shadow-evaluation`; preserved parent
`39b130fa34ba30b66558a6ceca5f1cb52b60cee1`, tree
`c016239e60a77681fb92c9bdabf13504d2243c0f`, itself a child of reviewed
`d058d674f0e2403c1015a44b1176d3bfc0449284`.
New publication subject: `review: document BB-132E Alpaca daily evidence rights gate`.
Resolve exact SHA/tree/parent using `git log --format='%H %T %P %s'` for that unique subject.

Only five Markdown files change: this report, `docs/reports/REPORT-CATALOG.md`,
`docs/STATUS.md`, `docs/BACKLOG.md`, `docs/operations/codex-recovery.md`.
No production, test, schema, configuration, dependency or runtime changes. Existing reviewed
characterization commits remain unchanged. Unrelated mockups and unpublished ADR0006–0009 excluded.

### Reusable ownership and smallest prospective extension

Inspected D's `MarketObservationEvidence.cs` and `FinanceMarketObservations.cs`, existing
provider mappings/entitlements, accepted daily science and the preceding reviewed characterization.
Finance's existing SQLite owner, trusted acquisition/ingestion clock, immutable revision checksums
and sealed-cutoff watermark remain the intended home. A daily source needs an additive/versioned
provider-observation and eligibility contract; it must not reinterpret existing Snapshot/OneMinute
rows. No separate Alpaca database, independent evidence framework or strategy engine is justified.

The prospective contract would bind explicit feed, raw adjustment, symbol/mapping policy,
canonical instrument/currency, New York source date, provider daily values, source commitment,
trusted acquisition/ingestion, revision lineage and eligibility version. IEX feed identity is not
the listing venue and cannot establish an instrument MIC by itself. A provider date is not knowledge
time. Exact re-observation must retain original knowledge; changed evidence needs a later immutable
revision. These are requirements for the pending implementation, not implemented guarantees for
Alpaca. Existing D receipts are not relabelled as daily evidence.

### Official source characterization

Public documentation/agreements retrieved **2026-10-03**; no market-data endpoint was called.

| Official source | Bounded finding and consequence |
| --- | --- |
| [Historical stock bars](https://docs.alpaca.markets/us/reference/stockbars) | Native `1Day` bars are available. Bind explicit `feed=iex`, `adjustment=raw`, bounded date range and symbol/asof policy rather than account/time-dependent defaults. Exhaust pagination before claiming coverage. No live entitlement or instrument mapping is inferred from endpoint availability. |
| [Market Data FAQ](https://docs.alpaca.markets/us/docs/market-data-faq) | Daily bars aggregate trades directly using New York day boundaries; daily and minute trade-condition rules differ. Daily volume can include extended-hours trades that do not update daily OHLC. Missing bars are not zero-valued sessions. IEX is one exchange, not consolidated US coverage. A native daily observation avoids inventing minute aggregation, but its source-day meaning must remain explicit. |
| [Real-time stock data](https://docs.alpaca.markets/us/docs/real-time-stock-pricing-data) | Daily bars can be emitted during the day with accumulated values; `1Day` alone does not prove a completed source day. Streaming/current-day evidence must not silently satisfy a historical completed-day eligibility rule. |

The target is a revisable **provider/feed daily observation**, not an eternally final price or a
claim of whole-market completeness. This review does not prove that Alpaca cannot support the
required daily semantics. It also does not finish a completed-source-session eligibility policy:
that implementation/scientific review remains pending behind the rights gate below. No elapsed-close
heuristic, guessed publication time or claim that the last observed bar is final was introduced.

### Storage evidence preserved; exact unresolved lifecycle question

The accepted [owner-supplied support evidence](finance-alpaca-owner-support-evidence-20260906.md)
records six explicit YES answers for private/non-commercial Basic/Free historical daily storage,
normalized local database, backups, checksums/revisions, accumulating daily history and derived
research results. Its publication was owner-approved. **These answers remain valid attributed
evidence; this review does not reopen them or claim private storage is prohibited.** Raw support
correspondence was not independently inspected and is not reproduced.

That same accepted report explicitly leaves post-account termination retention, cancellation/deletion
obligations and separate audit artifacts unresolved. Current public source inspection did not close
those specific gaps or establish which account/feed agreements govern the planned historical IEX use.

| Official agreement source | Relevant inspected scope; no broader inference |
| --- | --- |
| [Alpaca disclosure index](https://alpaca.markets/disclosures) | Current index links the Terms and the two subscriber agreements below. Being linked does not establish their applicability to an individual account/feed. |
| [Terms and Conditions](https://files.alpaca.markets/disclosures/library/TermsAndConditions.pdf) | Personal/non-commercial and content provisions distinguish use restrictions; termination/modification and additional-policy provisions remain applicable questions. No explicit permission covering retained evidence/replay after termination, nor a definitive deletion scope, was identified. Silence is not converted to permission or prohibition. |
| [NASDAQ OMX subscriber agreement](https://files.alpaca.markets/disclosures/library/NASDAQ+OMX+Global+Subscriber+Agreement.pdf) | Section 10 addresses termination and limits on receiving/using information. It does not resolve the planned IEX retention question; do not apply a different feed's subscriber obligations by assumption. |
| [NYSE subscriber agreement](https://files.alpaca.markets/disclosures/library/NYSE+Market+Data+Display+Services+Agreement.pdf) | Section 7 addresses duration and surviving provisions. It does not establish a retained-IEX-evidence grant. No automatic SIP/NYSE applicability is asserted. |

Exact downloaded PDF SHA-256 commitments (public sources only; PDFs not committed):

- Terms: `2dc774d4aeeafbe4c7f0565e7842d932bc8bc10488af805fce43b8734e7b9859`.
- NASDAQ: `54dc85b3e4a2a4d023a1de66373f0221f868ecfa8f7638ef122dade7e46a3b53`.
- NYSE: `d5a672e2895c69398b3064e16719354940aa6f585f3c4f22e058ff0a778933e2`.

Retrieval date, URLs, section names and file hashes pin the inspected evidence without asserting
an unverified effective agreement date or that an owner signed these exact documents.

**Concrete incompatibility with granting rights now:** existing
`FinanceMarketObservations.RequireObservationRights` requires zero cost, affirmative
HistoricalAnalysis/LongTermStorage entitlement, `RetentionClassification.LongTerm` and
`DeletionRequirement.None`, supported by explicit provider or owner-accepted personal-research
evidence. D cannot promise later subscription/deadline deletion of immutable receipts. The six YES
answers do not settle the already-recorded termination/deletion lifecycle. Assigning `None` now
would silently infer the missing fact; bypassing the guard or adding purge behavior would change
accepted lineage. Neither is done. Existing live Alpaca readiness is a different product boundary,
not a grant for this historical daily source.

### Smallest next evidence and remaining work

Owner/architect review should resolve the **applicable Basic historical IEX retention lifecycle**:
may lawfully acquired daily OHLCV, normalized copies, backups, immutable revisions/provenance and
associated scientific audit/results remain retained and replayable after account closure or data
entitlement termination? If obligations differ, identify the exact affected artifacts and deadlines.
Use applicable agreement/provider clarification or an explicit reviewed evidence interpretation;
do not infer an unrestricted grant from API access. No provider message was sent on the owner's
behalf and no credential is requested in chat. Alpaca selection and private storage's six answered
uses do not need approval again.

This is the user's explicit provider-rights STOP condition. Do not weaken immutable evidence,
use a fallback source or start implementation on a fabricated retention policy. After this gap is
resolved, completed-source-day eligibility, bounded adapter/fixtures, versioned additive persistence,
frozen research candidate and prospective non-trading result/replay still require implementation
and scientific verification. No E acceptance criterion is declared complete by this docs-only review.

### Verification of this review delta

Risk-based scope: public-contract/rights analysis and documentation only. Prior 61 focused
D/adapter/compatibility PASS and 27 daily science PASS are retained as dated evidence of unchanged
code/tests, not evidence that the new Alpaca/shadow capability exists. No API/Finance/Sentinel/frontend
suite, model, provider-data call or build is rerun solely for this publication. No migration exists.
Applicable publication results (2026-10-03):

| Command/check | Result |
| --- | --- |
| `node scripts/verify-documentation.mjs` | PASS:262 Markdown files,91 unique backlog IDs |
| `git diff --check`; `git diff --cached --check` | PASS |
| `git diff --cached --name-only` | Exact five Markdown files listed above; no unrelated material |
| `git diff --quiet HEAD -- src tests` | PASS: no production/test delta from the reviewed parent |
| Gitleaks8.28.0 `git . --pre-commit --staged --redact --no-banner` | PASS: no leaks |

No CI green is inferred. Public PDFs were inspected outside Git; only source links, sanitized
findings and artifact hashes are published. No credentials or raw provider market payloads exist
in this delta.

Finance **RESEARCH / 0 SEK / NONE**. No provider activation, Qwen, grant reset/refund/renewal,
PAPER/LIVE/AUTO, broker/orders/positions/capital, scheduler, automatic promotion, deployment or
BB-132F. STOP after publication for independent architect review; no merge.


## Owner-accepted daily shadow implementation — 2026-10-03

### OWNER-ACCEPTED DATA-USE/RISK DECISION

The product owner explicitly authorizes this private, non-commercial BigBrain installation to
retain lawfully acquired Alpaca evidence for local research, normalized market evidence,
prospective SHADOW, deterministic replay, immutable provenance/checksums/revisions, required
backups, derived scientific results and historical decision auditability. The owner accepts the
currently documented account/entitlement-termination and possible deletion uncertainty. This is
**owner risk acceptance, not an Alpaca contractual guarantee or legal finding**. The prior six
support answers and the unresolved contractual facts above remain unchanged. The lifecycle question
no longer blocks this authorized implementation.

If a concrete applicable retention/deletion requirement becomes known, affected acquisition and use
must fail closed until reconciled with immutable evidence architecture. No automatic deletion,
lineage rewrite or assumption that owner acceptance overrides a provider obligation is authorized.
The explicit internal policy uses `OwnerAcceptedPersonalResearch`, a versioned owner-evidence
reference and **Unknown** contractual deletion/post-subscription status; it does not fabricate
`ExplicitProviderGrant`. Current policy must be supplied again for daily projections/freezes/results.
Known deletion obligations or revoked affected use are rejected, including replay. No provider
activation, credential or trading grant follows from constructing this policy.

### Publication identity and implemented result

**MERGE CANDIDATE / IMPLEMENTED / AUTOMATICALLY VERIFIED / REVIEW CANDIDATE ONLY.**
Accepted baseline remains `8c09fd4dce4147766b51979ecd2cacc21d87eb92`, tree
`69ae54c5b3b2243c71bcbf9755a71383f31e1d8f`. Same branch
`bb-132e/prospective-shadow-evaluation`, preserved parent
`c114b60d1241623c0d91341d144b6963a599e6c3`, tree `a6596cb826f19e0847ec39747d84e393775eb42e`.
Both earlier characterization commits remain ancestors, without rewriting.
Unique publication subject: `review: implement BB-132E prospective daily shadow evaluation`.
Resolve that exact SHA/tree/parent via `git log --format='%H %T %P %s'`; no self-referential hash.
Earlier sections are dated reviewed history, not current blockers or approval requests.

The implemented path reuses existing observation receipts/knowledge watermark, daily feature engine,
momentum strategy, native research result reader/checksum, and accepted source-close shadow horizon.
It adds a bounded historical Alpaca adapter, explicit owner-risk policy, versioned daily envelope,
trusted immutable freeze and sealed-cutoff non-trading result. Migration97 adds only shadow
candidate/result tables to the existing Finance DB. No second market store, evaluator/ledger fork,
research grant, public endpoint or scheduler. [Canonical semantics and limitations](../../../architecture/finance/market-data-memory-and-provenance.md#bb-132e-daily-prospective-evidence--review-implementation-2026-10-03).

The complete proof uses deterministic fixtures only. It establishes an acquired, persisted daily
receipt; native supporting research; trusted freeze; a later-known source-day outcome; reopen;
then a later revision/observation while the original sealed projection and result remain identical.
The observed percentage is raw close movement, **not** net profitability or executable performance.
The existing momentum strategy receives an inert portfolio view because of its accepted interface;
this path never creates orders, positions, balances, fills or a backtest run.

### Acceptance evidence

| Required boundary | Model-free evidence |
| --- | --- |
| Native provider daily evidence | Alpaca-shaped transport fixture → existing Finance acquisition owner → v2 daily receipt; explicit IEX/raw/asof-disabled mapping, payload hash and UTC ingestion survive reopen |
| Completed source day | Versioned historical1Day plus strictly earlier New York source date at trusted request start; current/future day, midday timestamp, invented publication, mismatched contract and malformed OHLC fail; DST boundary cases |
| Trusted freeze/support | Freeze samples TimeProvider; persisted native research checksum/creation time and exact supported momentum parameters required; no caller timestamp/signal |
| Prospective knowledge | Pre-freeze projection yields no outcome; old event acquired after freeze is later knowledge; E before freeze or after trusted now fails |
| Revision/noninterference | Exact duplicate retains original identity/time; changed value without current predecessor fails; later revision/new day cannot alter serialized earlier knowledge projection or canonical result |
| Restart/immutability | Reopened candidate has identical serialized content/identity; same candidate/E returns identical result; candidate/support/observation/result corruption or missing dependency fails |
| Concurrency/crash | Concurrent freezes/results converge to one immutable row; competing revisions cannot fork; before-commit failure rolls back; migration97 rollback leaves96 and retry/reopen succeeds |
| Rights lifecycle | Policy records OwnerAcceptedPersonalResearch plus Unknown contractual deletion; known deletion/revoked use rejects acquisition and further query/evaluation, with zero fixture-source calls |
| Research/science isolation | Old spent v1 authority remains spent; B/C model-free regressions pass; shadow path does not call backtest/robustness/learning ledger/reasoner or existing scheduled shadow cycle |
| No external activation | Disabled adapter does not call transport; fixtures reject incomplete pagination/oversize/duplicate JSON/wrong symbol/date/auth response; cancellation persists nothing; real-model opt-ins removed for full suite |

### Verification — 2026-10-03

During implementation, four initial focused failures were fixture expectations (immutable-array
reference equality, exact exception subtype and schema96 expectation); these were corrected without
relaxing production validation. The initial compilation also identified a return-type analyzer
suggestion, corrected before final gates. No pre-existing correctness blocker was found.

| Exact command/check | Final result |
| --- | --- |
| `dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --configuration Release --no-restore --filter 'FullyQualifiedName~ProspectiveDailyShadowTests\|FullyQualifiedName~FinanceMarketObservationTests\|FullyQualifiedName~ProspectiveShadowCompatibilityTests\|FullyQualifiedName~TwelveDataMarketObservationTests\|FullyQualifiedName~FinanceFeature\|FullyQualifiedName~FinanceDeterministicBacktest\|FullyQualifiedName~ResearchLearningContract\|FullyQualifiedName~FinanceLearningLedger\|FullyQualifiedName~FiniteResearchSession'` | PASS221,0 fail/skip; includes33 new daily-shadow cases and existing temporal/science/governance regressions |
| `dotnet restore BigBrain.slnx` | PASS, existing dependencies up to date |
| `dotnet build BigBrain.slnx --configuration Release --no-restore` | PASS,0 warnings/errors |
| `dotnet format BigBrain.slnx --verify-no-changes --no-restore` | PASS, exit0/no changes |
| `env -u BB132A_LOCAL_ACCEPTANCE -u BB132C_LOCAL_ACCEPTANCE dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --configuration Release --no-build --no-restore` | PASS1014,0 fail;8 real-model cases SKIP; no inference |

The sandbox blocked MSBuild named-pipe creation, so test/build gates ran with approved process
permissions. No host services/configuration or deployment changed. Frontend/Sentinel suites were
not run because their code/contracts are untouched; solution Release compilation includes their
projects. No live Alpaca call, credential, new package or network-dependent test was introduced.
Final publication checks (same implementation tree):

- `node scripts/verify-documentation.mjs`: PASS,262 Markdown files/91 unique backlog IDs.
- `git diff --check` and `git diff --cached --check`: PASS.
- Exact staged scope:19 intended files; cumulative baseline scope21, including preserved prior
  characterization tests. No unstaged tracked delta or unrelated untracked files included.
- Gitleaks8.28.0 `git . --pre-commit --staged --redact --no-banner`: PASS, no leaks.
- No package, deployment, model/grammar/runtime, frontend, Sentinel or CI configuration change.
  No GitHub CI result is inferred from local verification.

### Exact current delta and limitations

Current delta:19 intended files, of which10 C#/test files and9 Markdown documents:

- `src/BigBrain.Modules/Finance/DailyMarketEvidence.cs`
- `src/BigBrain.Modules/Finance/MarketObservationEvidence.cs`
- `src/BigBrain.Api/Finance/AlpacaDailyMarketObservations.cs`
- `src/BigBrain.Api/Finance/FinanceProspectiveDailyShadow.cs`
- `src/BigBrain.Api/Finance/FinanceMarketObservations.cs`
- `src/BigBrain.Api/Finance/FinanceSchemaMigrations.cs`
- `src/BigBrain.Api/Finance/FinanceLearningLedger.cs` — schema97 recognition only
- `src/BigBrain.Api/Finance/FinanceFiniteResearchSession.cs` — schema97 recognition only
- `tests/BigBrain.Api.Tests/ProspectiveDailyShadowTests.cs`
- `tests/BigBrain.Api.Tests/FinanceMarketObservationTests.cs` — latest-schema expectation only
- `ARCHITECTURE.md`
- `TESTING.md`
- `docs/STATUS.md`
- `docs/BACKLOG.md`
- `docs/modules/finance.md`
- `docs/architecture/finance/market-data-memory-and-provenance.md`
- `docs/operations/codex-recovery.md`
- `docs/reports/REPORT-CATALOG.md`
- `docs/reports/features/finance/bb-132e-prospective-shadow-compatibility-20261002.md`

The cumulative baseline diff also retains the earlier `ProspectiveShadowCompatibilityTests.cs`
and `TwelveDataMarketObservationTests.cs` characterization additions:21 files total. Unrelated
mockups and unpublished ADR0006–0009 remain untouched/excluded.

Limits: first source IEX/raw1Day, one symbol/day per request, current-day bars denied, bounded1000
receipt projection; existing mapping/calendar limits, no symbol master or global-market completeness;
only supported momentum periods5/10/20; raw close/direction measurement, no costs/corporate-action
normalization or profitability claim. Supporting backtest evidence is retained identity, not proof
that its historical returns recur prospectively. Next eligible source day is not guaranteed next
exchange session. A source day can predate freeze in market time while first becoming knowable
later; evidence explicitly records both. Later revisions require a new cutoff/result; old evidence
is preserved. Operator must update current policy if concrete obligations emerge. No live provider
or deployed end-to-end runtime is claimed, and none is required for this fixture acceptance.

### Final handoff

STOP after publication of this exact Merge Candidate for independent architect review. Owner risk
acceptance authorizes data use within scope, **not merge of this implementation**. No merge,
deployment, provider activation, Qwen, scientific-grant renewal/reset/refund, PAPER/LIVE/AUTO,
broker/orders/positions/capital, scheduler/daemon, automatic promotion, frontend or BB-132F.
Finance remains **RESEARCH / 0 SEK / NONE**.


## Accepted publication — 2026-10-03

**ACCEPTED / MERGED / CI VERIFIED.** Independent architect review and explicit owner approval
apply to exact candidate `5e755c0c7e38c1e8843257b7222e3fe81b3c4f43`. The preceding Review Checkpoint
and Merge Candidate sections remain dated history, not outstanding approval requests.

- Verified baseline/first merge parent: `8c09fd4dce4147766b51979ecd2cacc21d87eb92`.
- Approved candidate/second merge parent: `5e755c0c7e38c1e8843257b7222e3fe81b3c4f43`.
- Merge: `47d369870a8c043902f47b563144899aa54f8774`.
- Candidate and merge tree, exactly equal: `ddfdcfe5ea8f59504f9bf99371b50172d7e29d95`.
- Pre-merge local/remote branch matched, merge-base equalled baseline,4 ahead/0 behind,
  no tracked work affected publication; unrelated mockups/ADR0006–0009 preserved.
- Normal non-rewriting merge/push; no candidate change, rebase, amend, squash or force push.

[Merge Actions run37123692743](https://github.com/T-bear/BigBrain/actions/runs/37123692743)
was independently checked as a completed SUCCESS push run on main for the exact merge SHA.
Actual jobs and steps, not just an overall badge:

| Job | Verified successful steps |
| --- | --- |
| backend | Checkout/setup-dotnet; solution restore; `dotnet format --verify-no-changes --no-restore`; Release build; solution tests |
| frontend | Checkout/setup-node; `npm ci`; `npm run format:check`; `npm test -- --run`; production build |
| documentation | Checkout/setup-node; `node scripts/verify-documentation.mjs` |
| secrets | Checkout; `gitleaks/gitleaks-action@v2` |

All jobs and their actual setup/verification/cleanup steps completed successfully. Unchanged
candidate-local verification was not repeated. No additional model or provider invocation occurred.

A separate minimal Markdown reconciliation records this durable acceptance and removes current
pending-acceptance wording in existing canonical documents. No implementation, tests, schema,
policy behavior or configuration changes. Exact reconciliation/final-main identity is resolved from
subject `docs: record accepted BB-132E checkpoint`; its own push CI is checked separately for its
SHA before handoff. Merge CI above is not substituted for that later commit's CI.

Finance **RESEARCH / 0 SEK / NONE**. Alpaca is not activated; no deployment, runtime/scheduler
start, Qwen, grant renewal, PAPER/LIVE/AUTO, broker/orders/capital or BB-132F. The owner-accepted
retention uncertainty and known-obligation fail-closed requirements remain unchanged.
STOP after accepted-main publication/verification; return control to owner/architect.
