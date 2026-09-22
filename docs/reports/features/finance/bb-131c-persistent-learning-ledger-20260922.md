# BB-131C — Persistent learning ledger

Detta är en sanerad GitHub-version.

## Metadata

- Baseline: `cb520994f3ae4222bab1ba31d8cc683abbc28991`.
- Branch: `bb-131c/persistent-learning-ledger`.
- Date: 2026-09-22.
- Finance: RESEARCH / 0 SEK / NONE.

## Status

**ACCEPTED / MERGED / CI VERIFIED**. Owner/architect-approved implementation merged unchanged. No deployment.


### Accepted publication evidence — 2026-09-22

Baseline / first merge parent: `cb520994f3ae4222bab1ba31d8cc683abbc28991`.
Approved candidate / second merge parent: `55a05bf967044f16aeb8d5482f0c158c09b2883f`.
Merge: `49072ea5832e0909e0ed9c77bc3ae9c98eee6b89`.
Candidate and merge tree: `c0942e47946c9572015e1d1320a25be0c6de6406` — identical content.
Pre-merge verification: exact remote SHAs, one ahead/zero behind, matching parent/merge-base,
and unchanged reviewed 13-file candidate. No implementation modification during publication.

[Exact merge CI 35767267447](https://github.com/T-bear/BigBrain/actions/runs/35767267447): **SUCCESS**.
All four jobs and their actual required steps completed successfully:

- Backend: checkout/setup, `dotnet restore BigBrain.slnx`,
  `dotnet format BigBrain.slnx --verify-no-changes --no-restore`,
  `dotnet build BigBrain.slnx --configuration Release --no-restore`,
  `dotnet test BigBrain.slnx --configuration Release --no-build`.
- Frontend: checkout/setup, `npm ci`, `npm run format:check`, `npm test -- --run`, `npm run build`.
- Documentation: `node scripts/verify-documentation.mjs`.
- Secrets: `gitleaks/gitleaks-action@v2`.

Reconciliation changes documentation only; accepted source/tests/schema/package/CI/runtime content
is unchanged. No deployment or real AI/provider integration occurred; scientific results and trading
authority are unchanged. Finance **RESEARCH / 0 SEK / NONE**. ADR 0038 remains Accepted;
BB-130 closed; A/B accepted; BB-131D **NOT STARTED / NOT AUTHORIZED**.

The separate reconciliation commit is identified from main by:
`git log -1 --format=%H --grep='^docs: reconcile accepted BB-131C persistent learning ledger$' main`.
Its exact final-SHA Actions run must also pass before publication handoff; GitHub commit/run history
and the final handoff identify that SHA and run without a self-referential documentation commit.

## Persistence characterization

Source inspected before schema changes:

- `EodhdMarketMemory` in `EodhdFinanceMarketData.cs` owns the configured Finance SQLite connection;
  it invokes `FinanceSchemaMigrator.Migrate` before legacy table initialization.
- `FinanceSchemaMigrator` records each version in a `BEGIN IMMEDIATE` transaction, rechecks
  concurrent migration winners, and rolls back DDL plus version recording on failure.
- `FinanceBacktestPersistence` owns one immutable result transaction including children;
  `FinanceRobustnessStore` owns one evaluation transaction. Their stores/readers stay authoritative.
- `FinanceAutonomousResearch` reserves with SQLite transactions and retains run history, but its
  key/single-flight/recovery semantics do not govern learning budgets or cross-proposal exposure.
- `FinanceDatasetIntakeStore` campaign tables share the configured Finance database. They are
  campaign evidence, not a substitute learning ledger. Existing partial-class/DDL debt is deferred.

There is no existing atomic transaction spanning learning admission, budget, exposure and results.
Reusing per-result writes does not manufacture that guarantee. A bounded additive migration in
existing migration ownership is appropriate; replacing existing owners or creating another DB is not.
The new reservation must commit before computation; result references are bound later, so failure
between those commits must remain spent/indeterminate. Scientific writers need no semantic change.

## Chosen boundary

One Finance-owned fixed synthetic protocol/family; no caller-selected new protocol IDs. Explicit
synthetic enrollment, one invocation, one proposal, three effective trials and at most 64 underlying
calls. A bounded governance snapshot with retained sanitized attempts belongs to the same SQLite
DB. Migration owns initialization; reads never recreate missing state. Exact synthetic cohort
membership may ignore revision labels, but unknown/overlapping alternatives fail closed.
No runtime route, worker, provider, production reasoner or new scientific engine.

## Evidence

Final stable-tree verification PASS: C 34/34, B 56/56, Finance regressions 74/74, schema/closure 4/4 (168/168 combined). Full API 754/754 and Sentinel 32/32; Release zero warnings/errors; full format verification exit 0. No skipped/failed final tests. See exact commands below.

## Changes

One new Finance ledger file, migration 94 in the existing migrator, one new focused test file and relevant canonical documentation only.

## Security

No AI/provider/data export or execution authority. Future model access still requires authenticated
principal/scope, Finance authorization, audit, resource/egress controls, export/retention rights and
security review. No deployment. Finance RESEARCH / 0 SEK / NONE.

## Remaining work

Independent post-merge verification and next-checkpoint planning remain. BB-131D is NOT STARTED / NOT AUTHORIZED. General real-market overlap adjudication and multi-protocol expansion remain deferred.

## Resumption

The exact approved candidate and merge identities are recorded above. No interrupted C implementation remains. STOP — return accepted BB-131C main to owner/architect for independent post-merge verification and next-checkpoint planning. No deployment or BB-131D.

## Implementation and authority

`FinanceLearningLedger.cs` adds typed governance and methods on the existing `EodhdMarketMemory`
partial class. `FinanceSchemaMigrations.cs` advances 93 -> 94 with one additive table,
`learning_governance`, and one singleton seed. No second DB, connection configuration, public API,
DI registration, worker, reasoner or generic event-sourcing/persistence framework is added.

The versioned, checksummed governance snapshot contains the fixed protocol/family, frozen
synthetic scope (plan/data/feature/eligibility snapshots), session-cohort digest, committed draft,
B input/proposal/execution identities, operational lifecycle, counters, result references and bounded
sanitized attempt history. The stored scope is **Finance-internal protected replay material**, not
reasoner input. B's development-only input and pure admission remain unchanged.

Only one fixed synthetic protocol/family can be enrolled. Caller-selected protocol/family IDs,
parent chains and arbitrary additional iterations are deliberately unavailable. Different model,
request, rationale, superficial revision names or calls to enrollment cannot create new budget.
This is durable governance for the accepted finite B protocol, not general adaptive learning.

### Migration and integrity

Migration 94 runs through the existing migrator's immediate transaction. Table creation, initial
UNKNOWN/uninitialized snapshot and version record commit together. No pre-existing Finance table,
row or identity is rewritten. Failure before version recording rolls back new DDL; old supported
version 93 and legacy evidence survive, then normal migration can retry. Fresh/reopened DB and
concurrent existing migration regressions are covered. Ledger reads never migrate, seed or repair
missing state. A deleted row with version 94 still recorded fails closed.

Schema must be exactly the supported ledger schema (94) and contract version. Snapshot reads
check full SHA-256, closed/case-sensitive JSON, numeric representation, duplicate properties,
known enums, bounded history, lifecycle/counter consistency, frozen scope, draft semantics and B
identities. An explicit ledger-local InstrumentId converter is needed for the new frozen input
round trip; existing types and scientific serializers remain unchanged. Checksums detect corruption;
they are not authentication against a privileged actor who rewrites the DB and all matching hashes.

### Transactions and state machine

Enrollment is an explicit trusted Finance-only assertion about this synthetic fixture, not a
reasoner-provided rights/exposure claim. Migration alone grants nothing. UNKNOWN enrollment cannot
be reenrolled as fresh. Production real-data enrollment/overlap decisions are not implemented.

- Uninitialized -> Ready: freeze the one explicit synthetic scope; no automatic enrollment.
- Ready -> AwaitingProposal: atomically spend the single reasoner invocation before the test fake.
- AwaitingProposal -> Reserved: B admission plus complete frozen population/budget/exposure commit.
- Reserved -> Running: commit the single start grant before returning the frozen scope for computation.
- Running -> Completed: verify already persisted immutable evaluation/backtests and bind references.
- Active work -> Failed or Indeterminate: preserve all spent authority; no automatic refund/retry.
- Indeterminate with an already committed start may bind exact persisted results without recomputation.
- Completed is immutable; exact completion replay is idempotent. Duplicate proposals link the original.

Every ledger mutation uses `BeginTransaction(deferred: false)` (SQLite immediate writer reservation),
reads authoritative state inside that transaction, validates the updated state and commits it before
returning authority. No in-process lock provides the guarantee. Two owners/connections cannot both
spend the last invocation or reserve the same experiment/cohort. Unsupported replacement scope may
terminate the one protocol invocation; it is never silently substituted for the frozen population.

The result writers remain separate existing transactions. There is intentionally no claim of an
atomic transaction across engine computation, all scientific writes and completion. A crash between
those boundaries leaves a spent reservation and potentially partial results, never fresh opportunity.
Explicit recovery marks uncertain work Indeterminate; constructors do not cancel another process's
active learning iteration. No clock timeout is treated as proof that computation did not happen.

### Accounting, exposure and history

Counters distinguish invocation allowance, submitted proposals (including rejections/duplicates),
three admitted effective momentum trials (5/10/20), conservative reserved underlying-call bound,
start grants, actual unique persisted run references and completed-result reuse. Start grants are
not falsely presented as exact CPU execution counts after a crash. B's preflight bound remains at
most 64; no plan is shrunk. Pending duplicates do not count as reused results.

Cohort identity covers instrument/session membership and omits revision labels, prices and features;
renaming/repricing the same sessions cannot make them unseen. Nonidentical membership is UNKNOWN,
not a new independent cohort. Frozen enrollment also prevents switching to a different scope under
the same program. General overlapping real-market windows, revised universes and economic-equivalence
adjudication remain unimplemented and fail closed; C does not prove statistical independence.

History retains admission rejection/no-useful/unsupported/rights/budget/exposure reasons, duplicates,
start/completion, failure and indeterminate states. Reasoner timeout/unavailable and synthetic risk
veto have closed failure codes. Received text is not stored for rejected output; bounded response
hashes and reasons are retained. Accepted B draft metadata remains bounded. Audit timestamps are
server-recorded UTC, independent of scientific fingerprints. History is capped at 256 entries and
snapshot reads at four million characters; exhaustion fails closed without erasing older history.
No general retention/deletion/compaction policy is introduced.

### Scientific stores, replay and risk

The ledger does not compute results. Tests obtain a committed start grant and pass the returned
frozen scope into unchanged `DeterministicRobustnessEvaluator.Evaluate`, existing momentum strategy
and underlying deterministic backtester. `PersistLearningResults` delegates to unchanged
`FinanceBacktestPersistence.PersistBacktest` and existing `PersistEvaluation` ownership.

Completion/reopen validates the stored evaluation plan, native payload checksum, row identity,
complete underlying-run set and existing run payload checksums/lineage before referencing them.
Ledger result data consists only of IDs/checksums and the existing native classifications, never
copied scientific result payloads or a new score. Missing/corrupt source results fail closed even
when the ledger previously recorded Completed. No MoreRobust -> RobustCandidate mapping exists.

Frozen replay from the reopened scope in tests uses no reasoner and reproduces exact result content
and identities. It is explicitly test-only reproduction of consumed engineering evidence, not a new
start grant. Exact writer/completion replay adds no scientific rows; conflicting checksums reject.
Current risk ALLOW is never fabricated. Native DENY/HALT/INSUFFICIENT_DATA fixtures terminate the
reserved research path, and ledger results retain RESEARCH/NONE/engineering-only authority.

## Recovery of this implementation

At resume, baseline/branch/HEAD remained exact; no C commit/staged/remote candidate existed.
Valid local work consisted of the migration edit, new ledger, 24-test class and characterization
report. No interruption note had been written; the canonical note was added before continuation.
Initial build passed with zero warnings/errors; 24/24 focused tests passed before later integrity
additions. Those pending additions were reverified (24/24), then coverage expanded to 34/34.
The new-code serializer issue and test analyzer/accessibility errors were corrected locally;
no pre-existing scientific/security/lineage defect was established. No existing work was recreated.

## Verification matrix

| Proof | Evidence |
| --- | --- |
| Migration | Fresh seed, version-93 upgrade, rollback before version record, idempotent reopen, legacy row preserved; existing concurrent migration tests |
| Atomic reservation | Exceptions before/after commit; reservation and invocation remain spent when committed |
| Crash/start | Lost start return cannot issue another grant; Reserved/Running recovery never refunds |
| Completion crash | Existing results survive failed ledger commit and bind without rerunning engine |
| Negative history | Malformed/unsupported/parameter/no-useful/rights/over-budget and reasoner failure survive reopen |
| Concurrency | Independent owners/connections contest invocation, duplicate reservation and same cohort with distinct fingerprints |
| Exposure | Consumed/unknown survive reopen, renamed revisions cannot renew, altered membership is unknown |
| Integrity | Missing row, unsupported version, malformed/duplicate/numeric-string JSON, checksum mismatch, invalid counters even with recomputed checksum |
| Results | Reopened native readers match full payloads; missing/conflicting rows reject; exact replay inserts no duplicates |
| Scientific boundary | B regression unchanged; native engine/results/risk semantics preserved; Web untouched |

Final source-tree commands, all exit 0:

```sh
dotnet restore BigBrain.slnx
dotnet build BigBrain.slnx --configuration Release --no-restore
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj --configuration Release --no-build --filter 'FullyQualifiedName~FinanceLearningLedgerTests|FullyQualifiedName~ResearchLearningContractTests|FullyQualifiedName~FinanceDeterministicBacktestTests|FullyQualifiedName~FinanceRobustnessEvaluationTests|FullyQualifiedName~FinanceResearchDatasetTests|FullyQualifiedName~FinanceResearchCampaignTests|FullyQualifiedName~FinanceAutonomousResearchTests|FullyQualifiedName~FinanceBacktestPersistenceTests|FullyQualifiedName~FinanceRiskEngineTests|FullyQualifiedName~FinanceClosureTests' --logger 'trx;LogFileName=bb131c-final-regressions.trx'
dotnet test BigBrain.slnx --configuration Release --no-build --logger 'trx;LogFilePrefix=bb131c-final'
dotnet format BigBrain.slnx --verify-no-changes --no-restore
node scripts/verify-documentation.mjs
git diff --check
git diff --cached --check
```

TRX confirms C 34, B 56, deterministic backtest 10, robustness 13, research datasets 9,
campaign 10, autonomous 15, immutable backtest persistence 4, risk 13 and closure/migration 4:
168/168 targeted. Full suite API 754/754 and Sentinel 32/32. SDK major 10/minor 0/patch 302.
Documentation/link verification PASS: 250 Markdown files, 91 unique backlog IDs. Gitleaks
8.28.0 history scan PASS: 287 commits, no leaks; staged candidate scan PASS, no leaks.
Working/staged diff checks PASS. Final pre-publication fetch retained the exact baseline.
Sandbox process restrictions required the same build/test/documentation commands to run with
approved execution permission; no application fix or verification policy weakening was used.

Only the three changed/new C# files received normal repository formatter output. No existing
scientific/B test source was edited. Explicit baseline diff for Modules, Web, workflow, package,
solution/build policy and deployment/runtime configuration is empty. No frontend suite rerun.
README/ARCHITECTURE/ADR/indexes/other modules/operations were assessed: no unrelated changes needed.
The complete candidate contains three C# files (ledger, migration, focused tests) and ten Markdown
files (report plus canonical status/backlog/testing/roadmap/module/contract/recovery/catalog).


## Limitations and rollback

This is a bounded local Finance implementation, not deployed/owner-runtime verified or accepted on
main. No production autonomous loop, real AI/model/network/provider/market data, external export,
statistical profitability, PAPER/LIVE/AUTO/broker/orders/capital authority is established.

Independent enrollment of future protocols, cumulative cross-protocol budgets, real-data overlap,
rights/retention governance, production history projection, authenticated callers, resource/egress
controls and full security review require new authorization. The internal snapshot must not be
sent to a future reasoner. Ledger presence is not permission to do that.

Crash tests simulate process interruption at deterministic transaction/computation boundaries with
independent SQLite reopen. They do not simulate storage-hardware failure or malicious/stale database
restore. Operational recovery must preserve the whole Finance DB/WAL; uncertain or replaced history
requires review and must not be declared fresh. No new backup/restore authority or automatic repair.

Rollback: stop using the new internal ledger methods and separately review any code revert. Preserve
migration 94 and all governance evidence; do not drop the table, delete rows, reset budget or restore
an older snapshot as a rollback shortcut. No destructive down migration is provided. No deployment
or production migration was run by this checkpoint; tests use isolated temporary databases only.
