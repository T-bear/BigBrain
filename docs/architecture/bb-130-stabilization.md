# BB-130 — platform stabilization, performance and continuity

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
task stopped before C. The owner has now authorized only the first C observation/cache/refresh
implementation checkpoint, then explicitly approved its publication and CI reconciliation.
That bounded checkpoint is published/CI verified; C overall remains partial. Characterization
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
