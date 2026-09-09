# BB-130 — platform stabilization, performance and continuity

## BB-130C exit approved — 2026-09-09

Owner and architect explicitly approved **BB-130C COMPLETE — ARCHITECT/OWNER EXIT APPROVED**
after review of `00a1fb1ab58a7db8ed1d05ff159f8cebd13b5359` and successful
[CI 34338386364](https://github.com/T-bear/BigBrain/actions/runs/34338386364).
BB-130A COMPLETE; BB-130B COMPLETE; BB-130C COMPLETE / EXIT APPROVED;
BB-130D NOT STARTED and requires separate authorization.
E1 and E2 remain ACCEPTED / MERGED / CI VERIFIED. No currently-known C blocking checkpoint remains.
Broader persistence/naming, composition/DI, sole schema/DDL authority, initialization/recovery,
structural hardening, reader extraction and other accepted post-BB-130 debt remain DEFERRED,
not implemented. C exit does not claim all original aspirational refactors were delivered.
No deployment, physical-device/runtime approval or new Finance authority is implied.
Finance RESEARCH / 0 SEK / NONE. Next: independent verification of this reconciliation in ChatGPT;
do not start D, Research Learning or Live Market Shadow.

## WHY / BASELINE / SCOPE

Owner priority is stabilization before further Finance features. Improve continuity,
deliberate module loading and maintainable ownership without changing scientific or
owner-approved behavior. Original source baseline:
`7fd89a5ccbe9be82699dc70950f461d3fbb6589c` (2026-09-05 review).
Each later checkpoint records its actual parent/source SHA in evidence and Git history.

Finance remains **RESEARCH / 0 SEK / NONE**. No broker, orders, PAPER/LIVE/AUTO,
capital allocation, Alpaca/provider activation, entitlement change or scientific
retuning. NOT EVALUABLE remains a valid outcome. No second database, backtester,
robustness engine, feature framework or research pipeline. No Redis, broker,
microservices, Redux, Kubernetes or speculative infrastructure. Preserve SQLite,
data, lineage, fail-closed gates and Sentinel boundaries. A discovered scientific
defect stops the refactor for documented owner/architect review.

## DECISIONS / STATE

Use the existing modular monolith, adapters and shared UI primitives. Every
extraction must own a verified responsibility, not just reduce line count.
The owner authorized this ordered sprint, BB-130A publication and subsequently the
BB-130B implementation/evidence and final documentation publication. The B publication
task stopped before C. At that point the owner authorized only the first C observation/cache/refresh
implementation checkpoint, then explicitly approved its publication and CI reconciliation.
That bounded checkpoint was published/CI verified while C was still partial; current C exit approval is recorded above. Characterization
precedes extraction; no backend work or next checkpoint starts automatically. See the
[bounded evidence](../reports/features/finance/bb-130c-observation-lifecycle-20260906.md). Deployment is a
separate action requiring explicit authorization. Sentinel security reconciliation
records gaps; it does not accept weaker requirements or start a security redesign.

This is the plan, not completion evidence. See [STATUS](../STATUS.md),
[BACKLOG](../BACKLOG.md) and the [baseline review](../reports/documentation/bb-130-architecture-code-review-20260905.md).
Phases are separately resumable and must end in coherent tested checkpoints.

## A — continuity and source of truth

Deliver the small canonical entry/read order, continuity contract, adaptive reasoning
policy in AGENTS, indexed sanitized architecture/code review and full B–D plan.
Reuse ROADMAP rather than creating a competing roadmap. Reconcile status/backlog,
BB-128C explicit owner evidence and architecture current/historical/future scope.
Record current application-auth and Sentinel hardening gaps without accepting them.

DoD: relevant documents agree, relative links/report metadata/BB IDs/diff and secrets
checks pass, code evidence is scoped honestly, and a coherent documentation commit
is published. No code or runtime change is needed. B/C/D wait for this checkpoint.

## B — loading and request architecture

1. Measure cold Home and Finance; include Media if its graph shows the same concern.
   Capture endpoint category (no identifiers/query secrets), start/end/duration,
   safely measured payload bytes, concurrency, triggering view and cache status
   where available. Distinguish fixture/browser, development and production evidence.
2. Classify CRITICAL (meaningful first render), SECONDARY (later hydration) and
   ON DEMAND (opened/relevant section). Measure before choosing limits.
3. Prefer progressive rendering, last-known-good state, lazy reads, deduplication,
   one-in-flight guards and avoidance of inactive-view global reads. Bound concurrency
   only where evidence warrants it. No mega-endpoint or new state framework.
4. Preserve BB-128B/C stale-while-revalidate, cache version/projection, online/visibility
   recovery, manual retry and one-loader semantics. Add trigger/timing regression tests.

DoD: reproducible before/after request graph and commands, measured counts/latencies
with limitations, improved loading behavior, focused/full Web tests and production
build, truthful docs and coherent publication. Browser timing is not automatically
physical-iPhone approval. Do not invent speedups or invoke provider/research mutations
to gather loading evidence.

## C — behavior-preserving code health

2026-09-09 [E2 campaign SQLite replay/reload characterization](../reports/features/finance/bb-130c-campaign-sqlite-replay-characterization-20260909.md):
**OUTCOME A / ACCEPTED / MERGED TO MAIN / CI VERIFIED**.
Approved candidate `c59f0200cf73bb936fb6f83ceaf603c0e1cf9d40` merged as
`f8f5df5f0c91688c58b2466cb276dda0dfa8c346`; [merge CI 34337618297](https://github.com/T-bear/BigBrain/actions/runs/34337618297)
passed backend, frontend, documentation and secrets on 2026-09-09.

One isolated regression preserves the complete campaign
row, six attempt identities/outcomes and 126 dataset rows through store reconstruction and replay.
No production change or blocker reproduced.
E1 and E2 are accepted. No currently-known C blocking checkpoint remains.
**BB-130C COMPLETE — ARCHITECT/OWNER EXIT APPROVED**.
C exit was approved by architect/owner on 2026-09-09. BB-130D remains NOT STARTED; accepted post-BB-130 debt
remains deferred, not implemented. Next action: independent publication verification; D requires separate authorization.
Accepted post-BB-130 debt stays deferred. No deployment, provider or scientific change.

2026-09-09 [E1 robustness identity correction](../reports/features/finance/bb-130c-robustness-selected-result-identity-fix-20260909.md):
**ACCEPTED / MERGED TO MAIN / CI VERIFIED**.
Approved candidate `b34178aa40024cb8e7e953c72706bd88b3dda688` merged as `1204322a8764df33c0182db5b1e98d3cd6912d81`.
[Main CI run 34312516174](https://github.com/T-bear/BigBrain/actions/runs/34312516174)
passed backend, frontend, documentation and secrets on 2026-09-09.
Existing evaluationId/checksum gates visible detail;
obsolete request completions cannot replace current evidence. Web-only bounded correction; no
scientific/schema change. Blocker `508cec3baceba4452df3431e7e0eab87dce50a3f` remains NOT MERGEABLE,
not correction ancestry and never merged. E1 and E2 are resolved/accepted above. C is complete / exit approved on 2026-09-09; D NOT STARTED.
No deployment, runtime or device UX approval is implied.
Accepted non-blocking debt remains deferred beyond BB-130. No deployment or provider authority.

2026-09-08 [reader characterization / accepted exit assessment](../reports/features/finance/bb-130c-backtest-reader-exit-assessment-20260908.md):
ACCEPTED / MERGED TO MAIN / CI VERIFIED; merge `796459be0f6a71fd3855a0cf2bc76f1bb2df68d5`,
main CI `34275678369` passed all four jobs. DO NOT EXTRACT is accepted; no production extraction.
The reader assessment identified E1 then E2. Both are now accepted above; no currently-known C
blocking checkpoint remains. C is complete / exit approved on 2026-09-09.
BB-130D is NOT STARTED.
The report's non-blocking debt is explicitly accepted for deferral beyond BB-130, including broader
extraction, composition and sole schema authority. This reconciles the original scope below;
it does not claim those goals were delivered or waive safety gates. Accepted E1/E2 evidence and
resolution of any reproduced blockers are required before D. Neither E1 nor E2 is authorized to
start by this acceptance; no next checkpoint or deployment starts here.

2026-09-08 [shared immutable writer](../reports/features/finance/bb-130c-backtest-persistence-writer-20260908.md):
ACCEPTED / MERGED TO MAIN / CI VERIFIED. Merge `e28b9850b0ee40f12a68d508f65c9e4c65d57085`;
main CI run `34247876050` passed all four jobs. One concrete Finance-owned four-table writer;
reference/robustness/research callers share it. No schema, connection, reader or scientific change.
Rollback/replay/conflict and deterministic JSON evidence are verified; no deployment.

2026-09-08 [Finance persistence characterization](../reports/features/finance/bb-130c-finance-persistence-characterization-20260908.md)
is accepted and merged as `e16c0c9b62f486955483b0ce782167f53e687714`; main CI run `34226093232` passed.
It maps shared SQLite ownership, distributed DDL/recovery and
provider-neutral storage under EodhdMarketMemory. No production extraction. The recommended next
scope was shared immutable backtest writing; its accepted and merged checkpoint is above.

2026-09-08 [CSV syntactic tokenizer](../reports/features/finance/bb-130c-intake-csv-syntactic-parser-20260908.md)
is accepted and merged as `a0f1ff2031016df72744574c5a4e2ee0810b3116`; main CI run `34192468991` passed.
Just raw line tokenization moves; the mixed ParseCsv
orchestration, domain validation and SQL remain in the store. No schema/identity change or deployment.

2026-09-08 bounded correction: [CSV optional-column presence](../reports/features/finance/bb-130c-csv-corporate-action-column-zero-fix-20260907.md)
retains first-column corporate-action evidence using existing lookup booleans. Accepted and merged
as `946eb98f139824e65780bf93d41686ea3724c1a3`; main CI run `34188274546` passed.
No schema/identity contract change, historical rewrite or deployment. This does not resume
CSV parsing extraction. The original blocker remains NOT MERGEABLE evidence, outside ancestry.

2026-09-07 first bounded intake extraction: [quarantine path/payload lifetime](../reports/features/finance/bb-130c-intake-safe-artifact-boundary-20260907.md)
was merged as `5a775b9f54784ea2f8562ea73299618ea7b0889f` and main CI run `34157988954` passed.
It isolates physical quarantine operations while preserving lifecycle, SQL, parsing, acquisition
and identity behavior. No schema authority moves. Remaining intake and other C/D responsibilities
remain separately authorized checkpoints; this change is not deployed.

2026-09-07 bounded correction exception: [canonical product and identity v2](../reports/features/finance/bb-130c-canonical-product-revision-identity-v2-20260907.md)
is merged and CI verified, not deployed. Owner-authorized Option B binds version + normalized
source + explicit product + canonical content, uses the same observation scope, and retains every
legacy ID. Existing manifests carry metadata; no DDL/migration/aliases. This corrects future
promotion identity only and does not resume intake responsibility extraction.

2026-09-07 bounded checkpoint: backtest catalog/result identity is isolated in
`useFinanceBacktestDetails`. The selected result must match the loaded `runId`; pending
and failed states render coherently without an old curve. Rapid stale completions are
ignored. Robustness remains separate pending review. Merged to main and CI verified in
`f0b4dbd73c50047c078227edff0bf9c0d5aa7bde` (Actions `34082759615`).

2026-09-06 next bounded scope: owner-supplied Finance source documentation and research-detail
characterization only unless a clean extraction is demonstrated. [Current characterization](../reports/features/finance/bb-130c-research-detail-characterization-20260906.md)
reproduced a selected-summary/prior-result display mismatch; the later identity checkpoint
corrected that defect. No additional backend or provider scope is authorized.


Work in small independently tested sub-checkpoints:

- Finance UI: observation/cache/retry hook, cohesive hero/overview/instrument/risk/
  research rendering and detail loading ownership as justified by actual code.
  Keep exact safety copy, accessibility, reduced motion and owner-approved layout.
- Intake: separate acquisition, safe files/quarantine, workbook/CSV parsing,
  validation, promotion and lifecycle/catalog persistence where responsibilities
  justify it. Preserve bounded input, sidecar semantics, rights and no automatic
  owner-artifact canonical promotion. Do not change the separate existing WIKI path.
- Finance persistence: move provider-neutral feature/backtest/robustness/risk/research
  ownership out of EODHD-specific naming/runtime responsibility. Keep one Finance
  SQLite database and provider-specific acquisition/mapping at the EODHD adapter.
- Composition: explicit small module registration extensions; preserve lifetimes,
  initialization/hosted-worker order and options. No reflection scanning.
- Schema: FinanceSchemaMigrator becomes sole structural authority; distinguish DDL
  from runtime reconciliation/data initialization. Preserve legacy and current data,
  migration order and concurrency. Test fresh, legacy, restart, rollback and concurrent
  initialization using isolated databases, never production Finance evidence.

DoD: characterization/regression passes, deterministic identities/checksums unchanged
for identical fixture inputs, full API and Sentinel/architecture tests, full Web tests,
Release build with zero warnings/errors, production Web build and repository gates.
If evidence changes unexpectedly, stop before altering scientific behavior.

## D — quality gates and reconciliation

Keep nullable, warnings-as-errors and latest-recommended backend analyzers. Evaluate
`dotnet format --verify-no-changes` or a compatible bounded equivalent. Add minimal
deterministic React/TypeScript lint/format verification only after the repository is
clean under its chosen scope. No unrelated mass rewrite or flaky rules.

CI retains backend restore/build/test, frontend install/test/build, documentation and
full-history secrets scanning. Run new gates locally before enabling CI. Reconcile
all phase states, remaining debt, security prerequisites, roadmap and report catalog.

DoD: all affected/full suites and new gates pass; no scientific/safety/UX contract
regression; reports distinguish implemented, CI, deployed and owner verified. Final
review can reconstruct purpose, source, measurements, changes, remaining work and
next sprint from GitHub alone.

## EVIDENCE / characterization map

| Contract | Existing tests to preserve |
| --- | --- |
| BB-127 eligibility/XLSX, BB-126 quarantine | FinanceResearchDatasetTests, FinanceDatasetIntakeTests |
| BB-128B/C cache/degraded/single loader | financeSnapshotCache.test.ts, FinanceObservation.test.tsx, shared component tests |
| BB-129A campaigns | FinanceResearchCampaignTests and campaign cases in FinanceResearchDatasetTests / FinanceEodhdIntegrationTests as applicable |
| BB-123 costs/execution identity | FinanceDeterministicBacktestTests, FinanceEodhdIntegrationTests |
| BB-124 OOS/holdout/integrity | FinanceRobustnessEvaluationTests, FinanceAutonomousResearchTests |
| Rights and fail closed | FinanceMarketDataEntitlementTests, FinanceAlpacaActivationReadinessTests, FinanceRiskEngineTests |
| Schema/restart/concurrency | FinanceClosureTests and affected persistence/store integration suites |
| Shell/widget loading | App.test.tsx, dashboard/widgetFramework.test.tsx, WidgetRegistry.test.tsx and new B request tests |
| Sentinel trust boundary | BigBrain.Sentinel.Tests, SentinelSystemMetricsProviderTests and architecture tests |

Run appropriate tests before each affected refactor to characterize the source. Record
exact commands and results rather than copying historical counts. Compose validation
is required if Compose or its docs are touched. Diff sanity, documentation and secrets
checks apply to every publication. Deterministic fixture evidence must not be confused
with new market evidence or a changed production database.

## REMAINING / NEXT / recovery

At each checkpoint put completed and remaining facts in STATUS/BACKLOG and evidence
in the indexed report. Use small phase/sub-phase commits, verify origin/main before
and after push, and never include unrelated local proposals/mockups. Preserve valid
working-tree changes. If interrupted, use only the [canonical recovery note](../operations/codex-recovery.md)
with baseline, changed files, exact tests already run and next safe action.

After A, next action is B measurement before changing request triggers. After B,
characterize and extract C responsibilities. After C, add D gates and perform final
independent review. After BB-130, recommend the existing security/authentication and
penetration-test prerequisite review before high-authority work; any further Finance
research expansion requires owner prioritization and the existing scientific gates.
