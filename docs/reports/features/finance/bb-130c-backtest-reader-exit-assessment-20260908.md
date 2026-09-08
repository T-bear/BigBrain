# BB-130C — Backtest reader characterization and exit assessment

Detta är en sanerad GitHub-version. Source inspection and isolated synthetic SQLite tests only.

## Metadata

- Date: 2026-09-08.
- Verified main baseline: `6bfcd011a655a7d9f225bc9c023063738d10db3b` (main CI `34248375970` succeeded).
- Branch: `bb-130c/backtest-reader-exit-assessment`, directly from that baseline.
- WHY: make the immutable read contract understandable and bound the remaining stabilization work.
- SCOPE: reader characterization and accepted C exit criteria; no production extraction.
- Authorities: ARCHITECTURE current boundaries; ADR 0021/0023/0024/0025; accepted
  [persistence map](bb-130c-finance-persistence-characterization-20260908.md) and
  [immutable writer](bb-130c-backtest-persistence-writer-20260908.md).

## Status

**ACCEPTED / MERGED TO MAIN / CI VERIFIED**. **DECISION B — DO NOT EXTRACT**.
Approved candidate `e868b601d511e17554452c24ac7ed9d591bf051f` merged as
`796459be0f6a71fd3855a0cf2bc76f1bb2df68d5`; [main CI run 34275678369](https://github.com/T-bear/BigBrain/actions/runs/34275678369)
passed backend, frontend, documentation and secrets on 2026-09-08.
No production files changed. Local test results below; no deployment or runtime claim.
BB-130C exit readiness: **NOT READY**; E1 and E2 are the only currently known remaining blocking
checkpoints. BB-130D is **NOT STARTED**.

The owner and architect accepted the shortened exit plan and explicit deferral of all non-blocking
debt classified below beyond BB-130. Original composition and sole schema authority goals are not
delivered; their accepted deferral reconciles scope without erasing the goals or waiving safety.
This acceptance does not authorize E1/E2 implementation or any subsequent checkpoint.

## Evidence

### Responsibility map

Source paths in this table are under `src/BigBrain.Api/Finance/` unless noted. Search covered
production `src` and `tools` references to backtest tables, BacktestCatalog, BacktestResult and
IFinanceBacktestReader; immutable-result reads are distinct from reading input bars/features.

| Read responsibility | Current owner / callers | Tables and provider scope | Behavior / boundary |
| --- | --- | --- | --- |
| Catalog | EodhdMarketMemory.BacktestCatalog in FinanceBacktestStore.cs; IFinanceBacktestReader wrapper, macro Analyze | backtest_runs.result_json; neutral | SQL ORDER BY strategy_id,cost_model,run_id; deserialize every full result; Summary projection plus fixed strategy menu and current generated time |
| Exact detail | EodhdMarketMemory.BacktestResult; EodhdFinanceBacktestReader.GetResult; macro Analyze | backtest_runs.result_json WHERE run_id=$id; neutral | Parameterized lookup, missing null; full JSON result is authoritative for reconstruction |
| Events/fills/equity for API and macro | Same detail reader | Arrays embedded in result_json | No SELECT from child tables to reconstruct them; preserves stored array order, does not sort/recalculate |
| HTTP views | FinanceEndpoints.cs /backtests and /backtests/{runId}; existing interface registered in Program.cs | Calls catalog/detail only | Missing detail 404; response pagination/clamping belongs to endpoint, not persistence |
| Macro regime analysis | FinanceMacroMemory.Analyze(EodhdMarketMemory) | Catalog/detail plus macro snapshot | Computes returns/buckets from adjacent equity points and explicit knowledge-time/regime policy. Scientific grouping stays outside an immutable reader |
| Robustness | FinanceRobustnessStore; ReadEvaluations / RobustnessEvaluation | robustness_evaluations and associated references; observations/feature_values for builds | Reads evaluation JSON with embedded run IDs; builds underlying backtests and writes through shared writer. Does not reload BacktestResult through catalog/detail |
| Bounded research dataset | FinanceResearchDatasets.RunBoundedResearchBacktest | research_dataset_observations/revisions, writer destination backtest tables | Calculates a result and returns it after shared persistence; no separate immutable backtest SELECT. Owner lineage embedded in configuration |
| Campaign | FinanceResearchCampaigns.ReadCampaign/catalog/build | research_campaigns JSON, research datasets | Current BB-129A results are categorical NOT EVALUABLE with null BacktestRunId. No backtest catalog/detail read or backtest calculation is performed here |
| Autonomous research | FinanceAutonomousResearch current-generation selection | robustness_evaluations, its children, feature lineage | Checks evaluation ID/checksum/source/feature and child counts; not a shared backtest reader. Selection and validation must not move into generic persistence |
| Risk/shadow/scheduler/operations | Their current Finance owners | risk/shadow/research/feature records | No immutable backtest-table read found in these paths. Reusing strategy types is not reading stored backtest results |
| Backup lineage | FinanceDataProtection.BuildPayload/RelatedIds/Rows | all four backtest tables; market_revisions_json, feature_revision_id | Raw selected row export including child tables. Provider-tagged retention traversal; does not deserialize BacktestResult. Backup checksum/restore drill is a separate contract |
| Retention/inventory | EodhdFinanceMarketData preview, deletion and retention counts; schema maintenance inventory | counts of four tables; inventory backtest_runs | Not a result reader. Deletion transaction/lifecycle authority remains separate; no call-site replacement justified |
| Writer conflict check | FinanceBacktestPersistence | backtest_runs.checksum | Reads solely for immutable replay/insert winner validation. Part of the accepted writer transaction policy, not catalog/detail ownership |

The only shared result read implementation is colocated in EodhdMarketMemory, not duplicated.
The interface adapter depends on M only for these reads. Macro Analyze's M argument is also used
only for catalog/detail reads in that method. Removing those dependencies fully would involve
connection/configuration/initialization ownership beyond a tiny open-connection SQL helper.

### Initialization, hidden behavior and serialization

Catalog/detail methods themselves open/dispose one connection and SELECT only. No DDL, provider
HTTP call, reconciliation, latest-feature selection or data mutation occurs inside these methods.
Constructing their EodhdMarketMemory dependency is different: it runs migrations, distributed DDL
and recovery writes before readers are usable. Provider disabled does not make construction read-only.
This distinction is already established by the accepted persistence map; tests initialize temporary
stores only. Runtime workers, options and provider authorization are not changed.

Stored results use `JsonSerializerOptions(JsonSerializerDefaults.Web)`: camelCase names,
case-insensitive property matching and Web number-reading handling, without the API enum-string
converter. BacktestResult includes Configuration, Metrics, Events, Fills, EquityCurve, Limitations
and ExecutionAttempts. Configuration contains source market IDs, feature revision, strategy and
parameters, cost/fill models, policy versions and optional research-dataset lineage. No separate
configuration_json column is read. The complete object graph reloads from result_json; SQL index
columns control catalog ordering, not reconstruction. Child arrays are redundant persisted evidence,
not joined back into the reader result. No hashing or numerical recomputation occurs on reload.

Catalog Summary computes the existing `Math.Max(0, GrossReturn - NetReturn)` CostImpact and formats
cost identity; it does not build a new scientific run. Fixed strategy descriptions and GeneratedAtUtc
are catalog presentation, not immutable row content. GeneratedAtUtc is deliberately excluded from
restart equality tests. No cross-culture collation redesign is proposed: SQL's existing text ordering
is retained, tested with existing ASCII identifiers and opposite insertion order.

HTTP serialization is separate: FinanceEndpoints adds camelCase enum strings. It slices Events/Fills
using nonnegative offsets and limits default 250 / clamped 1–500, and takes the first 500 EquityCurve
points. The bounded response is not a byte-identical full stored result and its RunId/checksum still
identifies the original complete evidence. This existing API policy is not changed or optimized here.

### Missing, malformed and conflicting evidence

Missing key returns null; interface delegates; endpoint returns 404. Invalid JSON syntax throws
JsonException from both catalog and detail, with no repair, fallback or skipping the bad row.
One bad payload therefore fails the catalog read; tests freeze this behavior, not a new availability policy.

These readers are deserializers, not full integrity validators. Source inspection shows no comparison
between JSON IDs/checksums/lineage and SQL metadata or physical child row counts, and no checksum
recomputation. JSON literal null can deserialize to null; missing members follow current record/
serializer behavior rather than an explicit schema-validation policy. Do not claim arbitrary incomplete
or structurally valid corrupted evidence fails closed. The accepted writer rejects conflicting
checksums on write; that is not proof of read-time corruption detection. No valid writer-produced
fixture violated lineage here, and no production corruption is inferred. Stronger read-time consistency
validation is explicit deferred hardening, to be defined before untrusted import/restore or new decision
consumers rely on these readers as validators; this checkpoint does not invent that policy.

### Characterization and reused evidence

Two new tests reuse the existing FinanceBacktestPersistenceTests temporary fixture:

1. `ReaderCatalogOrdersStoredRunsAndReopensWithoutChangingEvidence`: four engine-generated runs,
   reverse insertion, strategy/cost/RunId ordering, fixed strategy menu, RESEARCH, exact full result
   reconstruction, source/feature IDs, checksum and CostImpact projection, missing/SQL-shaped keys,
   catalog restart equivalence (except generated time), unchanged evidence snapshot, zero acquisitions.
2. `ReaderRejectsMalformedStoredJsonWithoutRepairingOrSkippingTheRow`: damage only a temporary
   result_json to `{`; both reads throw JsonException, missing remains null, snapshots unchanged.

Existing accepted tests already pin exact result/configuration/child JSON and stored JSON digest,
RunId/checksum, events/fills/equity counts, restart, equal replay, checksum conflict, rollback and
BB-123 concurrency. CanonicalDatasetRevisionIdentityTests proves non-EODHD reads with acquisition
disabled and empty credentials. These were reused, not reimplemented. ResearchDataset tests retain
research lineage; campaign tests remain policy coverage, not a new SQLite replay claim.

## Extraction decision

**DO NOT EXTRACT.** There is a coherent query/deserialization fragment, but it is small, implemented
once and already shared. Catalog construction also owns Summary projection, fixed strategy metadata
and generation time. A helper accepting an open connection would leave both current entry points,
constructor dependency and all consumers in place while adding forwarding calls. Moving the complete
catalog would move presentation ownership; removing the M dependency would broaden connection/DI
scope. No duplicated SQL/result reconstruction or blocked independent caller justifies that cost now.
Keep existing ownership documented. Reconsider only with a separately demonstrated independent consumer
or a separately authorized connection/reader policy boundary; no performance claim accompanies this decision.

## BB-130C EXIT ASSESSMENT

Every concern below has one classification. **Deferral is explicitly accepted, not a claim the original work
was completed.** Exit is sufficient stability and evidence for D, not zero technical debt. A new
reproduced defect would invoke the blocker workflow and change this assessment; none was reproduced
by the reader characterization. Synthetic malformed JSON testing is not production incidence evidence.

| Concern | Classification | Evidence / rationale / disposition |
| --- | --- | --- |
| Observation/cache/refresh and backtest UI result identity | ALREADY RESOLVED | Accepted lifecycle and result-identity reports; deterministic stale-response/loading tests. No broad rendering rewrite needed |
| Quarantine and CSV tokenizer ownership | ALREADY RESOLVED | Accepted concrete boundaries; bounds, lifecycle and column-zero correction tested |
| Canonical source/product v2, historical identity preservation | ALREADY RESOLVED | Accepted v2 tests; correction applies only to future promotions. Historical hash equality remains UNKNOWN, not required for v2 |
| Shared immutable backtest writer | ALREADY RESOLVED | Accepted four-table transaction, rollback, concurrency, exact stored JSON tests |
| Reader ownership/naming | ACCEPTABLE DEBT FOR POST-BB-130 | This map/tests and no-extraction decision; single shared implementation, no provider HTTP semantics |
| Provider-neutral persistence naming and remaining EodhdMarketMemory domains | ACCEPTABLE DEBT FOR POST-BB-130 | Accepted map identifies each responsibility; non-EODHD persisted evidence works without acquisition. Naming alone does not break the boundary |
| Finance database path/options ownership | ACCEPTABLE DEBT FOR POST-BB-130 | One SQLite path, EodhdFinanceOptions naming persists. Renaming does not unlock D and risks configuration compatibility |
| Schema/DDL sole authority | ACCEPTABLE DEBT FOR POST-BB-130 | Still distributed, not complete. Central migrator rollback/retry/concurrency and legacy preservation tests exist. No schema change required for D; future centralization requires its own high-risk authorization |
| Distributed initialization/recovery ordering | ACCEPTABLE DEBT FOR POST-BB-130 | Constructor order is mapped; current recovery tests protect known paths. Moving it now increases mutable-state risk without an exit defect |
| Whole-store concurrent construction / feature-robustness concurrent builders | ACCEPTABLE DEBT FOR POST-BB-130 | Unverified beyond specific migrator/backtest tests, not proven universally safe. Gate any future change introducing parallel construction/build ownership on targeted characterization |
| Legacy initialization variants | ACCEPTABLE DEBT FOR POST-BB-130 | Closure tests cover legacy marker preservation and retry, not every historical database layout. Broader coverage required before later schema/initialization changes; no production archaeology now |
| Intake reading/lexical values/validation/promotion/lifecycle/catalog and acquisition | ACCEPTABLE DEBT FOR POST-BB-130 | Remaining mixed responsibilities are documented; intake/protection/v2/XLSX suites preserve accepted behavior. No forced further extraction |
| File/SQL crash windows and archive/XLSX ownership | ACCEPTABLE DEBT FOR POST-BB-130 | Accepted intake map and bounded tests; generic fault injection before changing those boundaries, not speculative framework work now |
| Finance/module composition and DI registrations | ACCEPTABLE DEBT FOR POST-BB-130 | Program.cs remains explicit with known singleton/worker ordering. Registration extensions improve navigation, not a demonstrated safety or D blocker |
| Reader JSON cost, full catalog deserialization, endpoint response slicing | FUTURE OPTIMIZATION / NOT REQUIRED | Existing behavior mapped; no new measurement supports a cache/pagination/query redesign |
| Robustness persistence and macro provider/persistence coupling | ACCEPTABLE DEBT FOR POST-BB-130 | Own evaluation and regime semantics; moving them is higher risk than retaining mapped boundaries. Macro scientific grouping must stay outside mechanical reads |
| Backup/retention traversal and future artifact coverage | ACCEPTABLE DEBT FOR POST-BB-130 | Deliberate intra-Finance lineage traversal across four tables; protection tests. Any new artifact family requires explicit coverage, not generalized repositories |
| Research dataset persistence | ACCEPTABLE DEBT FOR POST-BB-130 | Existing isolated workbook/idempotency/lineage and writer coverage; no new pipeline proposed |
| Campaign SQLite persistence/replay | MUST CHARACTERIZE BEFORE BB-130C EXIT | Exit checkpoint E2 below; current policy-only test suite does not freeze the persisted aggregate that Research Learning will consume |
| Robustness frontend selected-summary/result coupling | MUST CHARACTERIZE BEFORE BB-130C EXIT | Exit checkpoint E1 below; accepted reports already flag analogous coupling and current source retains it |
| Other FinanceObservation rendering/secondary/detail effects | ACCEPTABLE DEBT FOR POST-BB-130 | Lifecycle and backtest identity isolated; cohesive-panel splits should follow a real product need. Excludes the E1 identity question |
| Nine on-demand detail requests / long-read samples | FUTURE OPTIMIZATION / NOT REQUIRED | BB-130B bounded cold-load triggers accepted; on-demand fan-out and incomplete long-read timing are documented limitations, not permission to change semantics |
| Read-time structural/identity/child consistency checks | ACCEPTABLE DEBT FOR POST-BB-130 | Limits stated above. No new trusted-write defect reproduced; require explicit policy/coverage before treating readers as integrity validators or widening ingestion trust |
| Auth/security for future high-authority Finance; Sentinel hardening | ACCEPTABLE DEBT FOR POST-BB-130 | ARCHITECTURE/ADRs retain known gaps. Mandatory prerequisite before trading, host controls or wider exposure; D/RESEARCH does not grant that authority |
| Historical WIKI compatibility/column-zero incidence, provider rights and future data expansion | ACCEPTABLE DEBT FOR POST-BB-130 | UNKNOWN stays UNKNOWN; no historical rekey/repair, provider activation or right inferred. Existing scientific eligibility gates remain mandatory |
| Deterministic format/static gates and final full reconciliation | FUTURE OPTIMIZATION / NOT REQUIRED | Not required for C exit because explicitly assigned to BB-130D; **mandatory in D**, not dropped from BB-130 |

Branch review, exact-SHA owner approval, main merge/CI and any release-specific manual verification
remain delivery gates, not deferred implementation debt.

No item is currently classified MUST FIX BEFORE BB-130C EXIT: this checkpoint has not reproduced
a new defect requiring a production correction. The two targeted characterization checkpoints may
change that conclusion and must stop for a separately authorized correction if a defect is reproduced.

### E1 — robustness display identity characterization

- Concrete risk: selecting evaluation B can leave separately held evaluation A detail state while
  the summary derives from selectedEvaluation. Current FinanceObservation.tsx has an unguarded
  `.then(setEvaluation)` effect and independently selected summary plus RobustnessDetails.
- Evidence: accepted backtest-result identity report's remaining question; current component lines
  around selectedEvaluation effect and robustness render. This is source-level evidence, **not a new
  reproduced runtime defect** in this checkpoint; no frontend change/test was performed here.
- Smallest checkpoint: synthetic A→B pending/success/failure, A→B→C stale completion and close/reopen
  characterization only. If misassociation is reproduced, publish blocker evidence and separately
  authorize the minimal correction; do not bundle new research-detail refactoring.
- Why before D: the analogous backtest defect was real and corrected in C. D should not certify a
  stable result presentation while this specifically identified scientific-identity question is open.

### E2 — campaign SQLite replay/reload characterization

- Concrete risk: policy tests can pass while persisted campaign definition, attempts, scorecard,
  dataset fingerprints or restart/idempotent identity differs. These aggregates are the next research
  evidence surface; current creation/read paths run their own table initialization.
- Evidence: FinanceResearchCampaignTests tests Population/Disposition only; current Run/ReadCampaign
  SQL and the accepted persistence map explicitly record this gap. BB-129A historical runtime replay
  does not provide an isolated regression on current main.
- Smallest checkpoint: temporary SQLite, deterministic synthetic research dataset, build campaign,
  reopen, replay same definition/time; assert one row, same ID/checksum/definition/results/scorecard,
  dataset lineage, all existing NOT EVALUABLE outcomes and null BacktestRunId. No acquisition or new
  campaign scientific behavior; preserve current DDL. Missing-ID behavior included.
- Why before D: a small repeatable test can close a concrete durable-evidence gap before quality gates
  are finalized and Research Learning relies on this aggregate. It does not justify a persistence rewrite.

## Changes

Production files: **none**. Two tests and optional synthetic result-fixture parameters in
`tests/BigBrain.Api.Tests/FinanceBacktestPersistenceTests.cs`; existing golden fixture defaults unchanged.
This report plus relevant TESTING, STATUS, BACKLOG, Finance module, stabilization plan, catalog and recovery.
Historical reports/ADRs retain their original evidence. No new BB ID assigned.

## Verification

Focused **84/84**, full API **663/663**, Sentinel **32/32**, zero failures/skips; Release **0 warnings/errors**

```sh
dotnet test tests/BigBrain.Api.Tests/BigBrain.Api.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~FinanceBacktestPersistenceTests|FullyQualifiedName~CanonicalDatasetRevisionIdentityTests|FullyQualifiedName~FinanceDeterministicBacktestTests|FullyQualifiedName~FinanceEodhdIntegrationTests|FullyQualifiedName~FinanceRobustnessEvaluationTests|FullyQualifiedName~FinanceResearchDatasetTests|FullyQualifiedName~FinanceResearchCampaignTests' --logger 'console;verbosity=minimal'
dotnet build BigBrain.slnx --configuration Release --no-restore
dotnet test BigBrain.slnx --configuration Release --no-build --logger 'console;verbosity=minimal'
node scripts/verify-documentation.mjs
git diff --check
git diff --cached --check
git diff --cached | docker run --rm -i --network none zricethezav/gitleaks:v8.28.0 stdin --redact --no-banner
```

Publication gates: documentation verifier passed **233 Markdown files / 90 unique backlog IDs**;
working/staged diff checks passed; staged Gitleaks v8.28.0 found **no leaks**. Only one test file
and eight documentation files are included; unrelated mockups and ADR proposals are excluded.

No Web/API production contract changed; no Web or Compose rerun required. Full API/Sentinel gates
are retained for this test/documentation checkpoint to verify fixture integration. Initial new test
code triggered existing CA1861/CA1859 analyzers; fixture declarations were corrected, no rule disabled.
No production inventory, repeated audit scan, performance measurement or external data access.

## Security

Finance **RESEARCH / 0 SEK / NONE**. No production code/DDL/schema/migration, historical evidence
rewrite, alias, identity change, production-data access, provider activation, acquisition, broker,
orders, PAPER/LIVE/AUTO, capital, deployment or new infrastructure. Existing BB-123/124/127/129A,
canonical v2/legacy and entitlement/provenance gates remain unchanged. Published evidence is synthetic
and sanitized. No new correctness/scientific/security/lineage defect reproduced here.

## Remaining work

The no-extraction decision and explicit exit deferrals are accepted. Remaining gaps are not proven
defects, nor proof all failure modes are safe. E1/E2 are accepted as the only known C blocking
checkpoints, but neither is authorized to start in this publication task.

## Resumption

Accepted main publication resolves the temporary review/recovery state. Use
[STATUS](../../../STATUS.md), [BB-130 plan](../../../architecture/bb-130-stabilization.md) and
[canonical recovery](../../../operations/codex-recovery.md). E1/E2 require separate bounded
authorization; do not start them or deploy automatically.

## BB-130C EXIT PLAN

- **Current exit readiness: NOT READY.** Accepted remaining blocking checkpoints (only currently known blockers), in order:
  1. **E1: robustness UI selected-result identity characterization** — close the specifically known
     cross-selection presentation question; separate correction only if a defect is reproduced.
  2. **E2: campaign SQLite replay/reload characterization** — freeze persisted aggregate/lineage and
     honest NOT EVALUABLE behavior with an isolated deterministic test.
- **Explicitly deferred beyond BB-130 by owner/architect acceptance**: additional reader and
  feature/robustness/risk/macro/intake extraction; shared path/options naming; composition extensions;
  sole DDL authority; broader startup/concurrency/legacy/crash matrices; reader integrity hardening
  before widened trust; JSON/request optimization; future artifact coverage; new data/provider work;
  auth/Sentinel hardening before high-authority use. No existing safety gate is waived.
- **Transition to BB-130D**: this assessment/deferral is now accepted and merged with green CI.
  Transition still requires E1/E2 evidence accepted on main, any reproduced blockers separately corrected/accepted,
  and the owner/architect confirms zero remaining C blockers. D then owns deterministic static/format
  gates, full regression and final documentation/runtime-verification reconciliation.
- Do not start another checkpoint now. Research Learning follows completed D and separate owner
  prioritization; no data eligibility, deployment, provider or trading authority is inferred.
