# BB-132F — Live market observation runtime

Detta är en sanerad GitHub-version. Review evidence, 2026-10-03. Synthetic transport fixtures only; no credentials, raw
provider payloads, private paths or model content. Current state: [ACCEPTED / MERGED / CI VERIFIED](#accepted-publication--2026-10-03).
The candidate-state text and verification below are retained as dated review history.

## Metadata

- Baseline/main: `c5fcd6712548c37f69453e237f17e6c0434b70ac`.
- Baseline tree: `3f868fb933ea33e55d662b00a1c041ce74ce4ec4`.
- Branch: `bb-132f/live-market-observation-runtime`.
- Publication subject: `review: implement BB-132F bounded market observation runtime`.
  Resolve exact review SHA/tree from that commit and remote branch; no self-referential hash.
- One bounded checkpoint, no merge/deployment/activation/next sprint. Preserved unrelated
  mockups and unpublished ADR0006–0009; none included.

Purpose: unattended acquisition of eligible daily market evidence through accepted Finance
ownership, without automatically evaluating science or trading.
[Authoritative runtime/configuration contract](../../../architecture/finance/market-data-memory-and-provenance.md#bb-132f-observation-runtime--review-implementation-2026-10-03).

## Status

**MERGE CANDIDATE / IMPLEMENTED / AUTOMATICALLY VERIFIED / REVIEW CANDIDATE ONLY.**
Not accepted, deployed, enabled or manually verified against a live provider. No production
universe/credential configured. Ordinary tests never require provider credentials/network/model.

## Changes

Existing `BackgroundService`/`SystemRecoveryCoordinator` is reused. The older
`FinanceProspectiveCadenceWorker` also starts features/shadow processing and is therefore not
reused as the Alpaca cycle. This runtime orchestrates only accepted acquisition; no general scheduler.
The existing Alpaca adapter already supplies fixed-origin1Day/IEX/raw transport, completed-source-day
validation, bounded receive and external secret configuration. BB-132D/E receipts, watermark,
canonical identities, effective mappings, rights policy and SQLite immediate transactions are reused.

A thin configuration DTO constructs existing Finance instrument/mapping types. Direct configuration
binding of immutable domain primitives failed a new test; the DTO corrects only configuration input,
not scientific semantics. No default or owner-approved production universe is invented.

`ReobserveDailyAsync` adds Finance-owned predecessor selection inside the existing transaction.
It reuses the same acquisition/validation/storage core as explicit-predecessor callers. Existing D/E
receipt identities and duplicate behavior are preserved. No migrations, second store or operational
cursor. The latest same content is Duplicate; changed content is New/Revision; stale-price reversion
extends lineage at current trusted time. Corrupted lineage/provenance never gets overwritten.

The singleton takes no queued work. Requests are serial and bounded by instrument/day count,
five-second spacing, adapter deadline/receive limits and cadence. Full cadence after restart avoids
immediate replay bursts. Existing recovery clock gating, monotonic sampled UTC guard and durable
receipt watermark prevent new knowledge from being backdated. No failed acquisition advances
scientific knowledge. No retry/refund/grant or automatic science/promotion path exists.

Internal snapshot + existing health architecture provide bounded status with finite failure categories.
Logs do not include exception text, payloads or credentials. No public mutation/status endpoint added.
The operational snapshot is intentionally ephemeral; receipt knowledge survives reopen exactly.

### Changed-file scope

Exactly12 intended files (5 code/test,7 Markdown):

- `src/BigBrain.Api/Finance/FinanceObservationRuntime.cs`: configuration, bounded coordinator, hosted worker and health.
- `src/BigBrain.Api/Finance/FinanceMarketObservations.cs`: shared owner acquisition core, transactional re-observation, clock preflight.
- `src/BigBrain.Api/Finance/AlpacaDailyMarketObservations.cs`: reuse credential validation, null fail-closed guard; transport semantics unchanged.
- `src/BigBrain.Api/Program.cs`: singleton/hosted-service/health registration, disabled by default.
- `tests/BigBrain.Api.Tests/FinanceObservationRuntimeTests.cs`: deterministic runtime/restart/failure/transport proofs.
- `ARCHITECTURE.md`: short ownership link.
- `docs/architecture/finance/market-data-memory-and-provenance.md`: canonical runtime/operator contract.
- `docs/STATUS.md`: current implementation/review state.
- `docs/BACKLOG.md`: bounded scope/known operational exclusions.
- `docs/operations/codex-recovery.md`: publication/continuity handoff.
- `docs/reports/REPORT-CATALOG.md`: report discovery.
- `docs/reports/features/finance/bb-132f-live-market-observation-runtime-20261003.md`: this review evidence.

No package, migration, model/runtime isolation, deployment/secret configuration, frontend,
Sentinel, strategy/science/risk or research grant file changes.

## Operational limit provenance

Official [Alpaca Market Data API plans](https://docs.alpaca.markets/us/v1.4.2/docs/about-market-data-api),
inspected2026-10-03, documents Basic historical API200requests/minute and IEX real-time equity coverage.
The runtime does not target that quota: max28 requests/cycle, at least5seconds between requests,
cadence60–1440minutes (default360). No quota assumption grants scientific eligibility or new rights.
Account-wide other clients are outside this runtime; later activation must check actual entitlement.
Provider terms/retention uncertainty remains the accepted E owner-risk decision, not a legal guarantee.
No paid feed, Twelve Data fallback, provider SDK, new data-use grant or live call.

## Evidence

Ordinary CI remains provider/model-free. Final unchanged-code confirmation completed successfully.

- Focused runtime + D/E tests:101 PASS,0 FAIL,0 SKIP.
- Restore: PASS; Release solution build: PASS,0 warnings/errors; full formatter verification: PASS.
- First full API run:1051 PASS,1 FAIL,8 intentional real-model SKIP (1060 total).
  Existing `LocalReasonerRuntimeTests.DeclineOrRuntimeFailurePreservesCInvocationWithoutEngineOrRetry`
  `no-useful` case returned null instead of NoUsefulProposal under its unchanged2-second proof-worker
  deadline. The failure is retained, not relabelled green. Isolated unchanged theory:5 PASS.
  A transient deadline/load interaction is plausible, not a proven root cause; no reasoner/test/deadline
  repair, exclusion, inference or grant change was made. One unchanged full confirmation completed:
  **1052 PASS,0 FAIL,8 intentional real-model SKIP**,1060 total (82seconds).
- Documentation/link verifier:263 Markdown files,91 unique backlog IDs PASS.
- `git diff --check`, exact staged12-file scope and staged Gitleaks8.28.0: PASS, no leaks.
- Local API/Finance verification is complete; branch GitHub CI is separate evidence, not claimed
  completed before the publication push. Main is unchanged.

Commands (Release; logs retained locally, sanitized results above):

```sh
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~FinanceObservationRuntimeTests|FullyQualifiedName~FinanceMarketObservationTests|FullyQualifiedName~ProspectiveDailyShadowTests' --logger 'console;verbosity=minimal'
dotnet restore BigBrain.slnx
dotnet format BigBrain.slnx --verify-no-changes --no-restore
dotnet build BigBrain.slnx -c Release --no-restore
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-build --no-restore --logger 'console;verbosity=minimal'
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~DeclineOrRuntimeFailurePreservesCInvocationWithoutEngineOrRetry' --logger 'console;verbosity=minimal'
# Then one unchanged full API confirmation with the same full-suite command above.
node scripts/verify-documentation.mjs
git diff --check
gitleaks git . --pre-commit --staged --redact --no-banner
```

Formatting was applied only to the five intended C# files before the full verification. The final
formatter check covered the solution. New focused runtime cases:38; existing D/E cases:63.


Initial sandbox MSBuild IPC was denied before tests could run; the same checks use authorized local
IPC outside the sandbox. Intermediate compile/test failures (duplicate initial DI insertion, test
accessibility/lifetime, configuration binder and source-exception categorization) were corrected
within the new implementation before final verification; no pre-existing defect was repaired.

Test coverage includes disabled/misconfigured credentials/policy/allowlist; real adapter fixtures;
New/Duplicate/Revision/reversion/reopen; sealed replay noninterference; provider malformed/current-day/
oversized/mismatched/HTTP/network/timeout failures; per-instrument isolation; bounded source-day plan
across New York DST; cancellation before commit and during HTTP; actual adapter deadline; hosted-service
shutdown; overlapping cycles; concurrent evidence owners; crash after commit and idempotent reopen;
clock regression across/in/reopened cycles; corrupted/missing evidence; and mapping-provenance refusal.

Existing market observation and daily-shadow regressions run alongside new tests. Shared API composition
and Finance receipt core justify a final full API suite and Release solution build/format. Frontend and
Sentinel suites are not locally rerun: their code/contracts are unchanged; no host-control boundary is
modified. No new migration: existing schema97/reopen regression is relevant, not invented schema tests.

## Security

The diff contains no credential, raw provider payload or private runtime evidence. HTTP doubles
retain DeterministicFixture origin; they cannot manufacture actually acquired provider receipts.
Only trusted configuration supplies canonical mappings, policy and external headers; fixed HTTPS
endpoint and existing no-proxy/no-redirect controls remain. Provider text/exception contents never
enter runtime snapshots or logs. Model/science/risk/ledger boundaries and accepted grant history are
unchanged. No Qwen, trading/order/position/capital, deployment or automatic shadow evaluation.

## Remaining work

- NOT DEPLOYED / NOT ENABLED / NOT LIVE-PROVIDER VERIFIED. Later separate owner authorization,
  credentials, current policy, explicit reviewed instrument mappings, clock recovery, storage and
  monitoring are required before operation. Never paste credentials into chat.
- Single API host only. No distributed scheduler/lease, infinite queue or operational cursor.
- Bounded recent window only; downtime beyond it is not caught up. Provider revisions outside it
  are not discovered automatically. No claim of complete coverage or permanently final provider bars.
- Existing receipt/projection caps remain; a capacity refusal is not permission to truncate evidence.
- Snapshot resets on restart; immutable receipts remain authoritative. Health does not prove live
  credentials or market-data availability until a separately authorized acquisition actually succeeds.
- Operator must reconcile any newly applicable deletion/retention obligation before affected use.
- No Qwen, research/science grant change, automatic shadow evaluation/promotion, PAPER/LIVE/AUTO,
  broker/order/position/capital, daemon outside the API host, frontend or deployment.

Finance **RESEARCH / 0 SEK / NONE**. STOP after publication for independent architect review;
only an exact reviewed SHA may later be owner-approved for merge. No next sprint.

## Resumption

Follow AGENTS.md and the canonical runtime contract linked above. Resolve this publication's exact
SHA/tree through its unique subject and compare with the recorded baseline. Independent architect
review and explicit owner approval of that exact SHA are required before merge. No further work,
provider activation, deployment or next checkpoint follows from publication.


## Accepted publication — 2026-10-03

Owner explicitly approved exact independently reviewed candidate
`a3ef3cd8a8ac5b1199a1cabc5001dc8d5ffff08c`. Pre-merge fetch verified local/remote candidate identity,
expected unchanged main,1 ahead/0 behind, exact merge-base and no tracked local modifications.
Unrelated mockups and ADR0006–0009 remain untouched and excluded.

- Baseline / first merge parent: `c5fcd6712548c37f69453e237f17e6c0434b70ac`.
- Approved candidate / second merge parent: `a3ef3cd8a8ac5b1199a1cabc5001dc8d5ffff08c`.
- Merge: `bef153d9f28a084ff2e50ff5580649ae30b055f4`.
- Candidate tree = merge tree: `1d121bb96f9fad8a097cf32655669d202c44af2a` (full-tree equality).
- Normal non-rewriting merge and push; no amend/rebase/squash/force push or candidate modification.

[Merge CI run37140171235](https://github.com/T-bear/BigBrain/actions/runs/37140171235)
was independently verified as SUCCESS for a push on main at that exact merge SHA. Actual jobs/steps:

| Job | Successful verification |
| --- | --- |
| backend | Restore, format verification, Release solution build, solution tests |
| frontend | npm ci, format check, tests, production build |
| documentation | node scripts/verify-documentation.mjs |
| secrets | gitleaks/gitleaks-action@v2 |

The reviewed implementation is unchanged; prior valid local test evidence is reused rather than
repeated. Candidate branch had no separate Actions run (workflow triggers main pushes/PRs);
the merge run above supplies exact-main CI evidence.

Only four Markdown files reconcile durable acceptance: STATUS, BACKLOG, recovery and this report.
No architecture/implementation/test/schema/runtime/provider configuration changes. The final docs
commit is uniquely resolved by subject `docs: record accepted BB-132F checkpoint`; its own push
CI must be verified for its exact SHA before final handoff. Merge CI is not evidence for that later SHA.

**ACCEPTED / MERGED / CI VERIFIED; NOT DEPLOYED / NOT ENABLED.** No Finance observation runtime
start, Alpaca credential/universe configuration or real request, automatic prospective evaluation,
Qwen, grant reset/refund/renewal, PAPER/LIVE/AUTO, broker/Avanza, orders/positions/capital or next sprint.
Finance **RESEARCH / 0 SEK / NONE**. STOP and return control to owner/architect; later live activation
and all remaining operational gates still require separate authorization.
