# BB-130C — Finance persistence responsibility characterization

Detta är en sanerad GitHub-version. Source inspection and isolated synthetic test evidence only.

## Metadata

- Date: 2026-09-08.
- Baseline main: `b1e84267a9909a55980e472bb6bd6054953d5588`.
- Branch: `bb-130c/finance-persistence-characterization`, directly from verified main.
- Scope: ownership/call/SQL characterization and one missing regression; no production extraction.
- Authorities: ARCHITECTURE; ADR 0021/0023–0025/0027–0028/0030; BB-123 cost/atomic-write,
  BB-124 holdout, BB-127 noncanonical datasets, BB-129A campaigns and accepted BB-130C identity/intake evidence.

## Status

**ACCEPTED / MERGED TO MAIN / CI VERIFIED**.
Approved candidate `888ee57fa0c25ee5d3a733c9e2bbff77294fb3aa` merged as
`e16c0c9b62f486955483b0ce782167f53e687714`; [main CI run 34226093232](https://github.com/T-bear/BigBrain/actions/runs/34226093232)
passed backend, frontend, documentation and secrets on 2026-09-08.
No production file changes, schema migration, production data access, deployment or runtime claim.
Finance **RESEARCH / 0 SEK / NONE**.
The recommendation below is not implemented or authorized for implementation by this report.

## Evidence

### Database and connection ownership

One configured Finance SQLite database is shared. `EodhdFinanceOptions.DatabasePath` under
`Finance:Eodhd` is the connection source for EodhdMarketMemory, FinanceDatasetIntakeStore,
FinanceDataProtectionStore, FinanceMacroMemory and FinanceAdjustedPriceAudit. Maintenance
commands also open it or initialize those owners. The EODHD-specific options name therefore
carries provider-neutral storage configuration. No new option/path or connection factory is proposed here.
Each operation generally opens/disposes its own SqliteConnection; singleton registration does
not mean one persistent connection or serialized database access. Migrate/State also open their
own connections. Current reader connections are ordinary SQLite connections, not read-only-mode
capabilities; semantic reads are distinguished from constructor/initializer effects.

`Program.cs` registers EodhdMarketMemory and the other stores as singletons. Feature/backtest/
robustness interfaces have EodhdFinance*Reader implementations delegating to that same object.
Risk, shadow, autonomous research, scheduler and operations also call that object directly.
Dataset/research/campaign readers delegate to FinanceDatasetIntakeStore; macro is separate.
No provider HTTP adapter is needed to construct storage or read existing evidence. Provider workers
retain their independent enable/account/entitlement/recovery gates; this does not waive rights.

`BigBrain.Modules/Finance/HistoricalDataPersistence.cs` is an in-memory reference/test contract;
its persistence tests are not evidence of a second deployed SQLite implementation. No production
API registration of IHistoricalDataPersistence/InMemoryHistoricalDataPersistence was found.
Immutable payload files, quarantine and provider-tagged backup/restore staging are existing
artifacts around the one database, not a second live Finance datastore.

### Responsibility / table map

All filenames below refer to `src/BigBrain.Api/Finance/`. **M** means the EodhdMarketMemory
constructor chain; **D** means FinanceDatasetIntakeStore initialization; **S** means the central
migrator. Test names refer to `tests/BigBrain.Api.Tests/`. Future owners are candidate responsibilities,
not a proposed generic repository hierarchy; only the final recommendation is a next checkpoint.

| Responsibility / current owner | Provider scope | Tables/data | Actual callers | Initialization dependency | Protecting tests | Candidate future owner / extraction risk |
| --- | --- | --- | --- | --- | --- | --- |
| Acquisition, EodhdAdapter + EodhdMarketMemory in EodhdFinanceMarketData.cs | EODHD-specific mapping, Free policy, watchlist, retention | acquisitions, payloads, EODHD observations/revisions, deletion_receipts, payload files | cadence worker, maintenance, observation reader | S then M | FinanceEodhdIntegrationTests: parsing/retry/store/replay/recovery/expiry/deletion | Keep acquisition/mapping at EODHD boundary; high risk if mixed with generic storage |
| Canonical market schema, same M owner; dataset writer in FinanceDatasetIntake.cs | Shared tables, source-tagged rows | observations, revisions, revision_price_capabilities | provider Store; dataset Promote; feature/backtest/robustness/shadow/readers | S capability table, M base tables before use | CanonicalDatasetRevisionIdentityTests, FinanceDatasetIntakeTests, FinanceClosureTests | Finance market storage authority later; high identity/schema risk |
| Feature storage, FinanceFeatureStore.cs partial M | Neutral exact-revision build; default input and UI watchlist remain EODHD-coupled | feature_definitions, feature_revisions, feature_values, feature_deletion_receipts; reads observations | BuildFeatures, feature reader/worker, cadence, backtest/robustness/risk/research consumers | M after base market tables | FinanceEodhdIntegrationTests durable/replay; canonical identity suite new non-EODHD test; FinanceDailyFeatureEngineTests | Finance feature persistence, but do not silently change default source or latest-selection semantics; high risk |
| Backtests, FinanceBacktestStore.cs partial M | Neutral serialization/storage; reference-build orchestration selects latest feature lineage | backtest_runs (configuration + result_json), backtest_events, backtest_fills, backtest_equity, backtest_deletion_receipts; reads feature/market | reference builder, robustness builder, FinanceResearchDatasets.RunBoundedResearchBacktest, reader | M backtest DDL; caller-supplied open connection for PersistBacktest | FinanceEodhdIntegrationTests concurrent convergence; FinanceDeterministicBacktestTests; new neutral/reload/conflict test | Concrete shared backtest writer is smallest next boundary; HIGH |
| Robustness, FinanceRobustnessStore.cs partial M | Neutral exact-lineage engine/persistence; latest-generation selection retained | robustness_evaluations (plan/result JSON), robustness_run_references, robustness_windows, robustness_parameter_sensitivity, robustness_cost_sensitivity, robustness_deletion_receipts | robustness worker/maintenance/reader; autonomous research; shared backtest writer | M after features/backtests | FinanceRobustnessEvaluationTests, FinanceAutonomousResearchTests, Eodhd deletion tests | Separate storage only after holdout/replay transaction coverage; high |
| Risk, FinanceRiskEngine.cs partial M | Engine has Finance policies, not provider strategy branches; storage hosted by M | risk_evaluations, risk_halt_state, risk_halt_audit; proposal source/feature/shadow IDs | risk endpoints/readers and shadow integration | M with validated FinanceRiskOptions | FinanceRiskEngineTests immutable evaluation/halt restart, policy and fail-closed cases | Risk evidence/halts require own scoped review; high safety risk |
| Shadow/cadence, FinanceShadowResearch.cs + FinanceProspectiveCadence.cs partial M | Current prospective runtime uses EODHD current bars; generic evidence concepts | shadow_predictions, shadow_outcomes, finance_cadence; reads features/observations, writes risk through M | cadence, shadow cycle, overview/endpoints | M feature/shadow/cadence/risk initialized before cycle | FinanceShadowResearchTests, FinanceRiskEngineTests | Leave coupled prospective sequencing intact; high |
| Autonomous research, FinanceAutonomousResearch.cs partial M | Neutral evidence/experiment records; readiness pins current runtime generation | research_runs, research_hypotheses, research_experiments, research_run_experiments, research_schema_versions; reads robustness/feature/market | orchestrator, scheduler, read endpoints | M after robustness/risk; startup recovery and local schema amendments | FinanceAutonomousResearchTests restart, partial failure, single-flight, current-lineage mismatch | Separate audit storage only after recovery/lease review; high |
| Scheduler/governor/operations, FinanceResearchScheduler.cs + FinanceResearchOperations.cs partial M | Finance runtime coordination; provider options participate in readiness | research_schedule_opportunities, research_operations, research_operational_incidents; research run references/resource-decision JSON | hosted workers, orchestrator, operations coordinator, endpoints | M after research; operations reconciliation also after system recovery | FinanceResearchSchedulerTests, FinanceResearchOperationsTests, FinanceResearchResourceGovernorTests | Runtime journals are mutable state, not immutable scientific results; high concurrency risk |
| Candidate/owner intake, FinanceDatasetIntake.cs + owner scanner | Provider-neutral claims, separate source-specific gates | dataset_candidates, dataset_candidate_files, dataset_corporate_actions; manifests, quarantine; Promote writes canonical tables | scanner, maintenance, dataset readers, cleanup/protection | D creates own tables; canonical operations require M/S tables | intake/protection/canonical identity suites | Remaining intake orchestration/SQL later; high lineage/rights risk |
| Noncanonical research data, FinanceResearchDatasets.cs partial intake | Source-neutral research eligibility; XLSX bounded owner path | research_dataset_revisions, research_dataset_observations; manifests; shared backtest_runs via M static writer | owner intake, research reader, bounded backtest, campaigns | D; extra columns amended locally; backtest use needs M tables | FinanceResearchDatasetTests: eligibility/workbook/idempotency/backtest/lineage | Do not merge with canonical data storage; high semantic risk |
| Campaigns, FinanceResearchCampaigns.cs partial intake | Neutral bounded campaign aggregate | research_campaigns definition/results/scorecard JSON with research revision fingerprints | campaign reader/maintenance | D; catalog/detail/run also invoke CREATE IF NOT EXISTS | FinanceResearchCampaignTests policy only; BB-129A historical runtime replay evidence; isolated SQL replay gap | Retain honest NOT EVALUABLE; later aggregate storage boundary, medium/high |
| Macro, FinanceMacroMemory.cs | Neutral macro/FX storage with FRED/European acquisition-specific methods | macro_revisions, macro_observations, macro_candidates; payload artifacts | macro readers/maintenance and provider client calls | S; constructor shares EodhdFinanceOptions path | FinanceMacroAndSessionTests and FinanceEuropeanMacroTests | Separate acquisition/storage only if separately scoped; high knowledge-time risk |
| Backup/retention, FinanceDataProtection.cs; M deletion helpers | Explicit provider/source/rights scope across derived evidence | canonical/candidate/feature/backtest/robustness/research/risk/shadow tables and receipts; backup JSON/manifests | maintenance/read inventory; cleanup and deletion preview/confirm | uses existing tables; backup constructor cleans incomplete staging files | FinanceDataProtectionTests and Eodhd deletion tests | Intentional cross-responsibility Finance traversal; never split without complete lineage inventory; high |
| Structural migration + adjusted audit, FinanceSchemaMigrations.cs | Neutral schema; audit has source-specific capability classification | finance_schema_migrations, macro tables, revision_price_capabilities; audit reads observations | M, macro constructor, maintenance/audit | independent S then M/D as required | FinanceClosureTests: legacy/retry/rollback/concurrent migration and adjusted denial | Sole structural authority is future work, not true today; high |

### Structural authority versus runtime initialization

FinanceSchemaMigrator latest version is **93**, with applied versions **1, 90, 91, 92, 93**.
Version 1 is a legacy marker (`SELECT 1`), not creation of all legacy Finance tables. Central
migrations create macro tables, revision capabilities and macro metadata/index changes. The
ledger table is created before per-migration transactions; each pending migration uses
BEGIN IMMEDIATE, rechecks the version under lock, executes DDL and records its version atomically.
The migrator explicitly configures a 30-second busy timeout. Its concurrent test does not prove
that every store initializer is safe under concurrent construction.

M constructor validates risk options, creates DB-parent/payload directories, runs S, then:
base market tables + WAL → features → backtests → robustness → shadow → cadence → risk →
autonomous research → scheduler → operations → acquisition interruption reconciliation.
This is actual call order, not an inferred order from filenames.

DDL outside S exists in every M domain initializer, D, research datasets and campaigns.
Autonomous research's AddColumn is also used by scheduler; it adds experiment metadata and
resource-decision JSON columns. Research datasets amend quality columns; D amends cleanup_state.
Those statements are outside the central migration ledger. Campaign read methods call DDL too.
Current `finance-schema-status` maintenance runs Migrate: its name does not imply read-only access.
None of these operations was executed against production for this checkpoint.

Initialization is not purely structural: D reconciles interrupted lifecycle/cleanup state;
autonomous startup backfills run links/attempt metadata and marks incomplete runs Failed;
scheduler reconciles Started opportunities without a matching run; cadence/operations insert
singleton defaults. Operations worker reconciles run/journal outcomes after system recovery.
Shadow lineage invalidation happens during RunShadowCycle, not InitializeShadowStorage.
M marks started acquisitions interrupted after its initialization chain. Preserve this ordering
and distinguish authorized recovery writes from immutable scientific rows during any future move.

DI singleton registration is lazy; registration order alone is not a global initialization barrier.
D accepts options, not an M dependency. Its constructor creates its own tables, not the entire
market/feature/backtest schema. Existing maintenance paths/tests explicitly instantiate M before
operations needing those tables. Hosted workers wait for system recovery, but constructor DDL
is earlier. Feature/backtest/robustness workers use timed delays (12/18/24 seconds) plus existing
provider gates, not a transactional dependency graph. This report does not change these gates.

### Transactions, concurrency and references

- Provider Store writes immutable payload files before its SQL transaction; filesystem and SQL
  are not one atomic resource. The transaction covers market rows/revision/acquisition evidence.
- Feature build computes first, checks for an existing ID, then inserts feature revision/values
  transactionally. Robustness similarly checks then inserts its evaluation/children. No claim of
  universal race-safe convergence is inferred for these builders from backtest/migrator tests.
- PersistBacktest accepts an already-open connection, checks checksum on existing run, then uses
  an insert-or-ignore transaction winner and verifies a losing writer's checksum. Events, fills
  and equity rows are committed with the winning run. Equal writes return false; conflicting
  checksum throws. BB-123 concurrent convergence remains a required preservation gate.
- Robustness persists underlying runs separately before evaluation. Whole multi-strategy batches
  are not one transaction. Restart/reuse semantics must not be replaced by a generic unit of work.
- Risk evaluation uses insert-or-ignore. Halt audit and mutable halt state are separate statements;
  this audit does not claim crash-atomic pairing or new concurrent-writer guarantees.
- Autonomous runs retain linked partial evidence; a unique partial index limits Running state.
  Scheduler claims use BEGIN IMMEDIATE and conditional updates; operational incident insertion and
  failure-count increment share a transaction. Recovery tests cover specific interruption points.
- Research dataset rows and revision are committed together; candidate manifests/state are separately
  orchestrated. Campaign aggregate is stored as one row with deterministic definition checksum;
  its current outcomes remain categorical NOT EVALUABLE rather than invented performance.
- Lineage is mostly application-maintained IDs/JSON references, not declared foreign keys:
  observations.revision_id; feature source-revision JSON and per-value source IDs; backtest
  feature_revision_id/market_revisions_json and result configuration; robustness references to runs,
  market and feature evidence; risk/shadow source/feature/proposal IDs; experiments to hypotheses/
  evaluations; campaign JSON to research dataset revisions/fingerprints. A shadow outcome REFERENCES
  declaration exists; no Finance PRAGMA foreign_keys setting was found. Do not assume cascade or
  referential enforcement without checking the actual connection configuration in a future task.

No non-Finance module was found querying Finance domain tables in the inspected SQL/registrations.
SystemRecovery uses its own journal and storage-health checks, not Finance scientific table mutation.
Cross-table calls inside Finance (comparison, builders, research shared writer, backup/deletion) are
real coupling, but do not alone violate the rule against *other modules* accessing module data.
Do not invent new submodule security boundaries or claim all coupling is a proven defect.

### Determinism, tests and gaps

Existing IDs/checksums and serialization remain byte-for-byte production-source unchanged:
canonical dataset v2 source/product/content and scale contract; historical legacy IDs; raw payload
EODHD identity; feature definitions/source revision/knowledge-time checksums; backtest configuration,
cost/fill v2 and child evidence; robustness plan/holdout results; risk proposal/policy IDs; research
hypothesis/experiment/run and campaign definition fingerprints. Source/provider scope and deletion
inheritance are part of the contract. NOT EVALUABLE remains a valid result, not an error to bypass.

One new test, `NonEodhdEvidenceUsesSharedPersistenceWithoutAcquisitionAndSurvivesRestart`, in
CanonicalDatasetRevisionIdentityTests reuses its existing synthetic WIKI fixture. Enabled and
AccountActive are false, token empty. Default BuildFeatures has no EODHD input and fails as before;
explicit canonical revision selection builds features. Six reference backtests reload exactly
across storage reconstruction, with original result JSON, market/feature IDs and row counts.
Equivalent shared-writer replay returns false; mismatched checksum throws without replacing evidence
or growing the four backtest table counts. Acquisition/EODHD observation counts remain zero.
This proves method-level persistence independence from acquisition, not production scheduling or
permission to operate a provider. The fixed v2 fixture identity is unchanged.

Existing protection: Closure tests cover legacy table preservation, migration rollback/retry and
concurrent S; Eodhd integration covers durable source/features, interrupted acquisitions, backtest
convergence and scoped deletion; research dataset tests cover workbook/idempotency and shared
backtest use; risk/shadow/research/scheduler/operations tests cover their immutable and mutable-state
contracts. Pure engine and in-memory persistence tests are distinguished from SQLite integration.

Remaining gaps before larger extractions: isolated campaign SQL persistence/reload coverage
(the campaign suite freezes policy, while BB-129A records historical runtime replay); generic crash injection between file and SQL phases;
concurrent whole-store construction with live work; feature/robustness concurrent equivalent builders;
full initialization equivalence on all historical schema variants; atomic rollback failure injection
for each child-table writer; broad cross-provider readiness and complete backup lineage coverage for
any newly added artifact family. These are unverified scenarios, not reproduced defect claims.
No new reproducible blocker was found or opportunistically corrected.

## Changes

Production files changed: **none**. One test and fixture access helpers in
`tests/BigBrain.Api.Tests/CanonicalDatasetRevisionIdentityTests.cs`; this report and relevant
TESTING/STATUS/BACKLOG/module/stabilization/catalog/recovery documentation only.

## Verification

New isolated characterization: **1/1 passed**. Focused suites **187/187**, full API **659/659**,
Sentinel **32/32**, zero failures/skips. Release solution build **0 warnings / 0 errors**.
Focused selection includes EodhdIntegration, Closure, DatasetIntake, DataProtection, CanonicalDatasetRevisionIdentity,
ResearchDataset, ResearchCampaign, AutonomousResearch, ResearchScheduler, ResearchOperations, RiskEngine,
ShadowResearch, RobustnessEvaluation and HistoricalDataPersistence test classes.

```sh
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~NonEodhdEvidenceUsesSharedPersistence' --logger 'console;verbosity=minimal'
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~FinanceEodhdIntegrationTests|FullyQualifiedName~FinanceClosureTests|FullyQualifiedName~FinanceDatasetIntakeTests|FullyQualifiedName~FinanceDataProtectionTests|FullyQualifiedName~CanonicalDatasetRevisionIdentityTests|FullyQualifiedName~FinanceResearchDatasetTests|FullyQualifiedName~FinanceResearchCampaignTests|FullyQualifiedName~FinanceAutonomousResearchTests|FullyQualifiedName~FinanceResearchSchedulerTests|FullyQualifiedName~FinanceResearchOperationsTests|FullyQualifiedName~FinanceRiskEngineTests|FullyQualifiedName~FinanceShadowResearchTests|FullyQualifiedName~FinanceRobustnessEvaluationTests|FullyQualifiedName~FinanceHistoricalDataPersistenceTests' --logger 'console;verbosity=minimal'
dotnet build BigBrain.slnx --configuration Release --no-restore
dotnet test BigBrain.slnx --configuration Release --no-build --logger 'console;verbosity=minimal'
node scripts/verify-documentation.mjs
git diff --check
git diff --cached --check
git diff --cached | docker run --rm -i --network none zricethezav/gitleaks:v8.28.0 stdin --redact --no-banner
```

Publication checks: documentation verifier **231 Markdown files / 90 unique backlog IDs**;
working/staged diff checks passed; staged Gitleaks v8.28.0 found no leaks. Only the intended
characterization test and eight documentation files are staged; production source is unchanged.
No Web production/API contract change, so no local Web rerun. No Compose/runbook change.
No production database or provider runtime was opened; no accepted audit inventory was repeated.

## Security

Finance **RESEARCH / 0 SEK / NONE**. One SQLite database; no schema/DDL/migration changes,
historical rewrite/rekey/aliases, provider activation, acquisition, broker/orders/PAPER/LIVE/AUTO,
capital allocation or deployment. Only synthetic temporary evidence; no sensitive paths, private
identifiers, raw market datasets or credentials in this report. Existing retention, entitlement,
scientific and fail-closed policies remain authoritative. Historical WIKI compatibility and historical
column-zero production incidence remain UNKNOWN; this task does not reopen those audits.

## Recommended next bounded checkpoint

**Extract only shared immutable backtest result writing from EodhdMarketMemory.PersistBacktest
into a concrete internal Finance-owned backtest persistence collaborator.** It already has three
verified caller families and takes an open SqliteConnection plus BacktestResult. Keep connection
ownership/lifetime with callers; no generic repository interface, new options, DI layer or datastore.

Likely files: FinanceBacktestStore.cs (writer + reference calls), FinanceRobustnessStore.cs (calls),
FinanceResearchDatasets.cs (call), one small FinanceBacktestPersistence.cs and focused tests.
Keep DDL initialization, catalog/detail readers, latest feature selection, builders, engine calculations,
JSON options/field representation, run/checksum conflict messages, transaction boundaries and all
four table row sets identical. Reuse existing atomic-winner test and new non-EODHD replay/conflict test;
add isolated child-insert rollback characterization before moving code if current coverage is insufficient.
No schema migration or legacy rewrite should be required. If it is, stop for separate review.
Risk: **HIGH**, because immutable IDs and multi-table transactional writes cross callers. HIGH reasoning
remains appropriate. This is a recommendation only; implementation requires separate owner/architect
scope authorization. Do not combine it with schema authority, reading performance or feature storage.

## Remaining work

Provider-neutral connection configuration/ownership, other feature/robustness/risk/research boundaries,
central schema authority and recovery ordering remain separate. Existing backtest catalog full-JSON
reading cost is historical BB-123 debt; no performance measurement/improvement is claimed here.
Further intake, composition, frontend detail debt and BB-130D are not started by this checkpoint.

## Resumption

This characterization is accepted and merged; temporary review/recovery state is resolved.
The recommended extraction still requires a separate owner/architect-authorized checkpoint.
Use [canonical recovery](../../../operations/codex-recovery.md) and
[BB-130 plan](../../../architecture/bb-130-stabilization.md). Do not implement or deploy automatically.
