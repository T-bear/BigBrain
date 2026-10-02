# BB-132E — Prospective shadow compatibility characterization

Detta är en sanerad GitHub-version. Synthetic fixtures only; no credentials, provider payloads,
private runtime paths or model content.

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
