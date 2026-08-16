# BigBrain Finance – master roadmap

BB-088 implements the bounded daily cadence and Finance UI v1.0 product hierarchy. The worker
separates internal recovery checks from actual provider calls, treats weekends/no-new-session as
healthy and preserves exactly-once/anti-backfill/clock gates. The read model owns market breadth,
signal aggregation and prospective metrics; Web renders backend truth in the durable order
`SUMMARY → EXPLANATION → RAW EVIDENCE`. Future PAPER/LIVE UI may add explicitly mode-labelled
portfolio/position information only after separate authorization; none exists now.

BB-087 implements the prospective foundation recommended by BB-086: canonical current EOD → causal knowledge snapshot → unchanged strategy → immutable `RESEARCH` shadow prediction → later append-only outcome. Deterministic identity, clock integrity, current-session age and anti-backfill gates protect the scientific claim. Historical backtests and prospective scorecards stay separate.

Near-term ordering: mature automatic daily current-EOD/outcome evidence → M5 Hard Risk Engine as justified → Security/Penetration Testing Baseline → only then separately authorized PAPER eligibility assessment. Security remains mandatory before LIVE, and no LIVE authority follows automatically from shadow evidence.

BB-086 completed 2026-08-15 with a scientifically/legal fail-closed result: WIKI contains no
SPY/QQQ/IWM and none of eight candidates passed both rights lineage and supported acquisition.
Historical bootstrap remains strong for five individual equities across 2014–2016 but insufficient
for multi-regime ETF comparison. Legitimate ETF history discovery is now opportunistic; the exact
next Finance slice should activate the existing provider-neutral prospective shadow contracts for
current EOD observations, still RESEARCH and without orders.

Planned sequence: historical research → deterministic strategies → hard Risk Engine → prospective
shadow observation → comprehensive security/pentest baseline → PAPER eligibility assessment →
PAPER → further validation → explicit LIVE gates. The security baseline combines continuous
automated checks with controlled black-/grey-box testing and is mandatory before real-money LIVE;
it is a strong gate before meaningful PAPER/execution authority. No stage self-promotes.

Prospective evidence is immutable: observation/knowledge timestamp, source and revision, causal
features, strategy/version/parameters, signal/prediction, confidence when defined and `RESEARCH`
mode are fixed at prediction time. Outcome evidence appends later. Historical research,
prospective evaluation and separately versioned parameter updates are distinct; observed outcomes
cannot silently rewrite predictions or strategy parameters.

BB-085 completed 2026-08-15: provider-tagged atomic backup, isolated restore/corruption
verification and quarantine cleanup preserve rights, retention and exact lineage. EODHD is
not copied into WIKI's indefinite backup class. Next is BB-086 bounded legitimate SPY/QQQ/IWM
history discovery/intake; prospective read-only observation follows repository evidence.

BB-084 completed 2026-08-15: provider-neutral quarantine, immutable manifests, deterministic
promotion and a bounded WIKI historical revision are deployed. Zenodo correctly remains
manual review. Longer WIKI evidence changes robustness sufficiency to MIXED/MIXED/FRAGILE.
BB-085 provider-tagged backup/restore and quarantine-cleanup verification is complete, not PAPER.

BB-083 temporarily supersedes the next Finance research slice. Future LIVE requires validated
host recovery, persistent order idempotency, duplicate prevention and broker reconciliation
after every unclean restart. This baseline covers host/data only.

Status: M0 and M1 complete; Finance remains an undeployed RESEARCH foundation.
Owner approval required: before every transition toward live or greater autonomy.
Live trading enabled: **NO**. Current trading mode: **RESEARCH** domain/module status only;
Finance is not deployed and no mode transition operation exists.

## What and why

BigBrain Finance is the planned, policy-governed path from reproducible research to
eventual autonomous trading. The objective is capital preservation and positive net
expectancy: small controlled risks, cost-aware opportunities, limited losses and
compounding over time. There is no guaranteed-return or daily-return target; 10% per
day is explicitly not a requirement. A correct decision may be to make zero trades.

Automatic trading is the destination, not the starting state. Real money cannot be
used before M10's prerequisites and explicit product-owner approval. AUTO cannot be
enabled before all M13 gates are evidenced and explicitly approved.

## Current state and next gate

- Current phase/milestone: RESEARCH / early M2, BB-045 in progress.
- Hard current budget: external Finance market data = **0 SEK** until explicitly changed.
- Completed: M0 specification and M1 domain/evidence foundation with read-only module status.
- BB-071: positive human evidence clears the submitted Twelve Data use on a qualifying paid
  Personal plan; Basic/free is insufficient. This is entitlement, not activation.
- Next safe task: resolve Alpaca Basic/free IEX entitlement for the same private scope, then
  compare any adequate authorized zero-cost option with Twelve Data as the paid fallback.
- Blocker for M2 ingestion: cost-first provider selection plus explicit product-owner
  activation approval. ADR 0021 authorizes no provider or ingestion.
- Safe parallel foundation: entitlement/provenance, identity/normalization,
  session/gap/replay, immutable correction/supersession assembly and synthetic acquisition
  gate/journal orchestration are verified.
  BB-072 has researched eligible zero-cost historical sources. Provider payload ingestion,
  persistence and account work remain blocked.
- BB-074 early read-only observation UI is implemented under M2 with a fail-closed
  provider-neutral snapshot. M8 remains planned and has not started.
- No free US or Nordic source is authorized. The later milestone is FIRST AUTHORIZED MARKET
  DATA INGESTION and requires a separate product-owner prompt.
- The former BB-071 State B is superseded by direct Twelve Data human evidence for Personal.
  No plan, account, key, adapter or data exists. Basic is not an operational option. Alpaca
  entitlement research is prepared but not sent; provider selection remains open.
- BB-075 fresh zero-cost sweep: no source passed the exact automation/storage/retention/
  replay/backtest gate. No adapter or real data was activated. Alpaca Basic/free IEX remains
  the next human-confirmation action; EODHD Free Starter is second. Twelve Data is inactive
  under the zero budget.
- BB-076: ADR 0022 permits capability-scoped owner acceptance for legitimate 0-SEK personal
  read-only research. Stooq daily history reached that evidence class, but its official CSV
  surface returned a JavaScript verification control; no bypass, adapter or ingestion occurred.
- BB-077: current `EODHD Free` is cleared for bounded private EOD research while active.
  Adapter, durable local memory, replay, API/UI and one-month termination deletion workflow
  were implemented/tested at the credential boundary.
- BB-078: the configured free credential enabled exactly eight successful no-retry requests.
  Production memory now holds 2,008 real daily observations across eight immutable revisions
  for 2025-08-11–2026-08-10. Restart/idempotence, exact-revision replay and API/UI are
  runtime-verified.
- BB-079: `core-daily-v1` produced immutable revision `feature-5d0397a53d094a2f` with
  42,168 causal feature values from all eight real market revisions. Determinism,
  no-lookahead, persistence/restart, API/UI and EODHD deletion lineage are runtime-verified.
  BB-080 subsequently delivered the minimal M3 research backtest bound to exact market + feature revisions; live remains a separate entitlement/source gate.
- BB-080: buy-and-hold, SMA10/20 and momentum20 now run through deterministic next-open, whole-share and explicit zero/conservative cost models. Immutable SQLite runs/journal/fills/curves/metrics, read-only API/UI, no-lookahead tests and EODHD deletion lineage are deployed and restart-verified. Next: BB-081 robustness/out-of-sample foundation, not PAPER.
- BB-081: chronological split/embargo, fixed expanding walk-forward, bounded parameter and five-level cost sensitivity, transparent scoring and insufficiency override are now immutable, deployed and restart-verified. Current 26-session OOS evidence is insufficient. Next: longer/second entitled history or adjusted corporate-action lineage; not PAPER.
- BB-082: the 2026-08-12 zero-cost history reassessment is legitimately blocked. Stooq's
  active browser-verification control prevents supported unattended CSV acquisition;
  EODHD Free adds no depth and relevant Alpha Vantage/Nasdaq Data Link history is paid.
  No data or runtime changed. Next: supported Stooq access with clear retention, or a named
  verifiably open historical artifact; never implicit provider stitching and never PAPER.
- Blockers for real money: M1–M9, accepted paper evidence, legal/operational research,
  broker sandbox evidence, secured credentials, reconciliation, emergency controls and
  explicit owner approval.

## Mandatory promotion gates

- No PAPER before deterministic backtesting, a hard Risk Engine and versioned strategies.
- No MANUAL_APPROVAL live mode before tested broker integration, secured credentials,
  reconciliation, emergency stop and explicit owner approval.
- No LIMITED_AUTO before accepted paper evidence, verified failure handling, risk limits,
  audit trail and explicit owner approval.
- No AUTO before accepted limited-live evidence, no unresolved critical defect, proven
  reconciliation and circuit breakers, and explicit owner approval.
- Time alone never promotes a mode. Every transition is an audited policy decision.

## Milestones

Each milestone is independently gated. “Rollback” means returning to the preceding
safe mode or disabling the new capability; it never means erasing financial evidence.

### M0 – Architecture and safety specification — COMPLETE

- Objective: establish the canonical Finance architecture and safety model.
- Scope/tasks: ADRs, threat model, module/controller boundaries, modes, roadmap, backlog,
  test strategy, runbooks and sanitized planning report.
- Non-goals: code, data feeds, broker selection, credentials, deployment or orders.
- Prerequisites: repository architecture and security conventions.
- Architecture impact: proposed Finance boundary inside the modular monolith.
- Tests: documentation links, schema, formatting and Compose validation.
- Security/docs: no secrets or runtime identifiers; all planning artifacts indexed.
- Definition of Done/gate: canonical documents published and validation green.
- Rollback: revert documentation commit; runtime is unaffected.

### M1 – Finance domain skeleton — COMPLETE

- Objective: create versioned domain primitives and a read-only module surface.
- Scope/tasks: money/instrument/time semantics, portfolio and signal value objects,
  module registration, in-memory fakes and explicit dependency directions.
- Non-goals: broker SDK, market ingestion, persistence, execution or AI decisions.
- Prerequisites: M0 and review of ADR 0017.
- Architecture impact: first-party Finance code in module/API boundaries, not Brain.
- Tests: unit/property tests for precision, currency, identifiers and serialization.
- Security/docs: no credentials; update module/API and data-ownership documentation.
- Definition of Done/gate: deterministic contracts, no write capability, green tests.
- Rollback: remove module registration and domain-only types; no schema or data migration exists.

### M2 – Historical market-data foundation — IN PROGRESS

- Objective: ingest reproducible, licensed historical datasets with provenance.
- Scope/tasks: provider adapter, corporate-action/timezone handling, quality checks,
  immutable dataset/version references, retention/allowed-use policy, decision-evidence
  lineage and a measured self-hosted storage choice.
- Non-goals: live feed, strategy claims or orders.
- Prerequisites: M1 and completed licensing/provider research for selected data.
- Architecture impact: Finance-owned storage and provider adapter.
- Tests: fixture ingestion, gaps, duplicates, stale timestamps, timezone and replay.
- Additional invariants: unknown/expired entitlement denies use; derived artifacts inherit
  lineage/restrictions; corrections append; deletion covers licensed copies/backups.
- Security/docs: minimize account/provider data; document license and provenance.
- Definition of Done/gate: identical dataset version produces identical normalized data.
- Rollback: disable adapter and retain referenced datasets/evidence per policy.

BB-090 closure evidence (2026-08-16) strengthens this active epic without completing all future data packs: Finance SQLite now has one ordered transactional migration coordinator with legacy bootstrap and lock serialization; Macro artifacts use quarantine/hash/rights/schema/validation/promotion evidence; and market revisions have explicit raw/adjusted capabilities. Production backup, isolated migration drill, deployment and restart idempotence passed. The remaining BB-090 gate is the owner-configured secret plus bounded first-party FRED/ALFRED vintage drill and published CI verification. BB-091, security/pentest, PAPER and later epics are not activated.

BB-046 research is complete. The provisional primary candidate is Twelve Data for
Nordic/global EOD, with Tiingo/Massive as US specialists. Public terms now show deletion
requirements after cancellation and leave parts of deterministic backtest/derived-data
use product-dependent. This does not start M2: BB-071 must establish an adequate written
entitlement first.

Provider-neutral BB-045 progress through 2026-08-11 includes fail-closed entitlement and
provenance, stable canonical instrument identity, effective-dated provider symbols, daily
decimal OHLCV, cash dividends, exact stock splits, basic quality findings and deterministic
synthetic normalization/duplicate handling. Explicit timezone-safe fixture sessions,
closure/unknown/missing/provider-gap semantics and single-revision no-lookahead replay are
also verified. Immutable parent/member assembly now preserves original→replacement correction
availability and old-revision reproduction. A fixture-only acquisition boundary now binds
request/batch/pagination/provenance, requires all storage/backtest/derived entitlements,
journals stable outcomes and reuses normalization/revision/replay. Measured storage,
authorized real adapters and external acceptance remain incomplete.

### BB-072 – FREE HISTORICAL DATA INGESTION preparation/research — COMPLETE

Ten zero-cost/free-adjacent candidates were researched using current first-party terms and
technical evidence. The comparison covers
license/ToS, local retention, personal backtesting, reproducibility, rate limits, history,
survivorship/delisted coverage, corporate actions, raw/adjusted state, symbol history and
quality. No source passed the complete durable-retention/non-display-backtesting gate, so
the decision is `DO NOT INGEST YET`. EODHD Free Starter and Twelve Data Basic remain
conditional evidence leads only. This milestone created no account, key, adapter, download
or persistence. BB-071-class exact entitlement and explicit owner review remain mandatory.

### Next safe M2 gate after Twelve Data entitlement confirmation

The synthetic acquisition, persistence-manifest/benchmark and live-observation/shadow-
learning foundations are complete. The live slice adds four-clock knowledge semantics,
honest freshness, deterministic gap/outage/correction delivery and immutable prospective
prediction/outcome evidence without an order path. Twelve Data Personal is an entitlement-
cleared paid fallback; Basic is insufficient and no provider is selected.

JSONL and SQLite were measured at up to 1,260,000 fixture rows; immutable files plus a
transactional SQLite catalog/index is the provisional direction, with medium confidence.
The immediate next milestone is **RESOLVE ALPACA BASIC/FREE IEX ENTITLEMENT**. If exact
rights are positively verified, compare it with the Twelve Data entitlement-cleared paid
fallback and pause for **PRODUCT OWNER APPROVAL – FIRST MARKET DATA PROVIDER**. If free
entitlement evidence remains absent, a separate
synthetic local-memory backup/restore validation remains safe, but is not started here.
Only after an
exact provider entitlement is accepted may an explicitly owner-approved **FIRST AUTHORIZED
MARKET DATA INGESTION** milestone begin.

### M3 – Backtest engine — IMPLEMENTED FIRST BOUNDED SLICE

- Objective: deterministic, cost-aware portfolio and order simulation.
- Scope/tasks delivered by BB-080: event clock, next-open full simulated fills, commission/slippage, benchmark, portfolio, metrics and reproducible reports. Partial/rejected fills, FX and tax-fee assumptions remain future robustness work.
- Non-goals: claiming future profitability or using live execution.
- Prerequisites: M2 and the BB-079 immutable feature-revision foundation (complete).
- Architecture impact: simulation ports isolated from future broker ports.
- Tests: golden replays, no look-ahead, deterministic seeds, cost and fill edge cases.
- Security/docs: dataset/report lineage; no private brokerage data.
- Definition of Done/gate: reproducible gross and net results with documented assumptions.
- Rollback: invalidate affected report versions; never rewrite prior evidence.

### M4 – Initial deterministic strategies — PLANNED

- Objective: independently test momentum, trend, breakout, mean-reversion and
  volume/liquidity candidates without assuming profitability.
- Scope/tasks: common StrategySignal, versioned parameters, invalidation/evidence,
  regime annotation and multi-strategy candidate aggregation.
- Non-goals: AI discretion, automatic promotion or authorization to trade.
- Prerequisites: M3.
- Architecture impact: pure strategy interfaces; agreement remains evidence only.
- Tests: unit, property, replay, parameter boundary and negative-signal cases.
- Security/docs: explanations contain safe evidence, not secrets or raw credentials.
- Definition of Done/gate: every strategy independently reproducible and disableable.
- Rollback: suspend/retire a version while preserving its reports.

### M5 – Hard Risk Engine — FOUNDATION IMPLEMENTED (BB-089, 2026-08-16)

- Objective: enforce non-bypassable exposure, loss, liquidity and health policy.
- Scope/tasks: per-trade/portfolio/instrument/sector limits, position count, daily loss,
  rolling drawdown, consecutive loss, spread/reward-risk, hours, stale data, volatility,
  broker health, sizing, exits, suspension and circuit breakers.
- Non-goals: strategy ranking or AI overrides.
- Prerequisites: M1 contracts and M3 simulation harness.
- Architecture impact: mandatory server-side gate below all proposers.
- Tests: policy matrix, adversarial bypass, invariants and `TRADING_DISABLED` transitions.
- Security/docs: configuration authorization, immutable evaluation and change audit.
- Definition of Done/gate: no execution path exists around risk/policy validation.
- Rollback: HALTED; revert policy version only through audited owner action.
- Current evidence: `research-eod-v1` enforces server-side RESEARCH proposal evaluation, immutable
  lineage, EOD freshness, health, price/move, rolling-volatility, volume-liquidity, sizing and
  simulated daily-loss/drawdown/loss-streak breakers. Durable halt/recovery audit survives restart.
  Sector, spread and aggregate portfolio rules remain explicitly not evaluable; no execution exists.

### M6 – Strategy Lab and evaluation — PLANNED

- Objective: compare evidence and govern strategy lifecycle.
- Scope/tasks: metrics, attribution, in/validation/out-of-sample splits, walk-forward,
  sensitivity, regime/cost stress and Monte Carlo sequence-risk analysis where useful.
- Non-goals: promotion from recent wins or optimization to one historical sample.
- Prerequisites: M3–M5.
- Architecture impact: read/report surface over versioned evidence.
- Tests: leakage/split guards, metric correctness and reproducible comparisons.
- Security/docs: provenance and limitations; historical results carry warnings.
- Definition of Done/gate: lifecycle states EXPERIMENTAL, BACKTESTED, PAPER, APPROVED,
  ACTIVE, SUSPENDED and RETIRED require explicit evidence and approval.
- Rollback: suspend strategy and preserve journal/report history.

### M7 – Paper trading engine — PLANNED

- Objective: exercise strategies in live or near-live conditions with simulated capital.
- Scope/tasks: restart-safe cash, positions, orders, fills, fees, delays, partial/rejected
  orders, daily summaries and strategy attribution.
- Non-goals: any route to a live-order adapter.
- Prerequisites: M2–M6 and PAPER gate satisfied.
- Architecture impact: paper execution adapter structurally separated from live adapter.
- Tests: integration, restart, clock, outage, duplicate, reconciliation and soak tests.
- Security/docs: unmistakable PAPER identity and evidence-retention policy.
- Definition of Done/gate: meaningful owner-accepted evidence, not a count of winning days.
- Rollback: stop simulation, snapshot state and restart from a verified checkpoint.

### M8 – Finance dashboard and UI — PLANNED

- Objective: show portfolio, P&L, mode, risk, positions, orders, signals and decisions.
- Boundary: BB-074's early read-only market observation view does not satisfy or start M8.
- Scope/tasks: overview, Strategy Lab, journal, warnings, broker health and emergency UI;
  WHY, WHY NOW, risk, costs, size, stop, target and portfolio impact in previews.
- Non-goals: client-side policy, secrets or hidden PAPER/LIVE distinction.
- Prerequisites: stable read models from M5–M7.
- Architecture impact: first-party Web module using versioned Finance API.
- Tests: component/e2e, accessibility, responsive, stale/error and PAPER/LIVE confusion.
- Security/docs: least disclosure; destructive actions require explicit confirmation.
- Definition of Done/gate: keyboard/screen-reader usable and modes visually unmistakable.
- Rollback: hide Finance navigation; backend remains safe and unchanged.

### M9 – Broker abstraction and evaluation — PLANNED

- Objective: select no broker until evidence supports a secure, compliant adapter.
- Scope/tasks: Sweden availability, API/sandbox, instruments, fees/spreads/data, order
  types, fractions, limits, authentication, reliability, account/legal restrictions and
  automated-trading terms; define adapter and paper/live credential separation.
- Non-goals: convenience-based choice, credentials or live account connection.
- Prerequisites: legal/operational research and M1/M3 contracts.
- Architecture impact: typed broker port behind Trading Controller.
- Tests: contract tests against fake then sandbox, rate-limit and failure injection.
- Security/docs: secret injection, least privilege, rotation/revocation threat review.
- Definition of Done/gate: documented owner-approved selection and sandbox evidence.
- Rollback: disable/remove adapter configuration; no strategy depends on vendor types.

### M10 – Manual approval trading — PLANNED

- Objective: permit exact owner-approved previews to reach the live adapter.
- Scope/tasks: observe, analyze, risk-check, preview, bind approval to immutable order,
  execute once, independently verify and reconcile.
- Non-goals: unattended entry or silent mode promotion.
- Prerequisites: M5, M8, M9, credentials/reconciliation/emergency stop and owner approval.
- Architecture impact: narrow Trading Controller capabilities and approval store.
- Tests: stale preview, changed price/size, duplicate, timeout, uncertain execution,
  partial fill, rejection, restart and authorization.
- Security/docs: strong approval/audit; credentials remain outside UI/AI.
- Definition of Done/gate: sandbox plus deliberately authorized live acceptance evidence.
- Rollback: HALTED, revoke live capability and reconcile broker truth.

### M11 – Limited live automation — PLANNED

- Objective: test unattended policy execution with deliberately tiny real exposure.
- Scope/tasks: narrow instruments, tiny sizes, low daily-loss ceiling, mandatory exits,
  anomaly suspension, notifications and daily reconciliation.
- Non-goals: normal AUTO limits or broad market universe.
- Prerequisites: accepted paper evidence, M10 evidence and explicit owner promotion.
- Architecture impact: policy-gated LIMITED_AUTO transition only.
- Tests: production-like failure drills and controlled rollback/reconciliation exercises.
- Security/docs: change control, on-call/notification and capital-loss acknowledgement.
- Definition of Done/gate: bounded long-duration evidence and no unresolved critical defect.
- Rollback: immediate HALTED/MANUAL_APPROVAL, cancel eligible pending orders and reconcile.

### M12 – Long-duration validation — PLANNED

- Objective: assess reliability and net expectancy across time and regimes.
- Scope/tasks: soak evidence, regime/cost drift, operational incidents, reconciliation,
  strategy attribution and drawdown review.
- Non-goals: promotion because a calendar interval elapsed.
- Prerequisites: M11.
- Architecture impact: evidence only; no broader authority.
- Tests: long-running, failover, data drift and recovery exercises.
- Security/docs: sanitized periodic reports and explicit residual risks.
- Definition of Done/gate: owner accepts representative evidence against predefined limits.
- Rollback: remain or return to LIMITED_AUTO/MANUAL_APPROVAL/HALTED.

### M13 – Policy-governed AUTO — PLANNED

- Objective: allow autonomous entry, management and exit strictly within policy.
- Scope/tasks: policy-authorized sizing/orders/exits, forced stops and ongoing verification.
- Non-goals: unrestricted authority, AI risk overrides or self-promotion.
- Prerequisites: M12 accepted, no critical defects, proven reconciliation/circuit breakers,
  legal gates current and explicit owner approval.
- Architecture impact: AUTO state enables existing capabilities; it creates no bypass.
- Tests: invariant, adversarial, chaos, reconciliation and emergency-stop certification.
- Security/docs: promotion record, current policy, loss limits and rollback runbook.
- Definition of Done/gate: independently reviewed evidence and explicit owner activation.
- Rollback: STOP ALL TRADING to HALTED, preserve safe exits per policy and reconcile.

### M14 – BigBrain Autonomic integration — PLANNED

- Objective: expose Finance through OBSERVE–DIAGNOSE–DECIDE–POLICY–ACT–VERIFY.
- Scope/tasks: capability discovery, structured proposals and safe decision explanations.
- Non-goals: direct AI-to-broker communication, credential access or policy mutation.
- Prerequisites: stable Finance capabilities and accepted ADRs through M13.
- Architecture impact: Brain calls the same Trading Controller as other clients.
- Tests: prompt/tool abuse, authorization, policy denial and evidence-chain integrity.
- Security/docs: AI output untrusted; no secret/account data in prompts.
- Definition of Done/gate: AI cannot bypass controller, risk, approval or reconciliation.
- Rollback: disable Finance capabilities in Brain without disabling core Finance safety.

### M15 – Continuous strategy governance — PLANNED

- Objective: monitor drift, retire weak strategies and safely evaluate replacements.
- Scope/tasks: review cadence, performance/risk alerts, champion/challenger evidence,
  suspension, retirement, model/data lineage and recurring legal/provider review.
- Non-goals: automatic promotion from recent performance or percentage-risk escalation.
- Prerequisites: M6 and any active trading mode.
- Architecture impact: governance over immutable versions and audit evidence.
- Tests: drift alerts, lifecycle transitions and revoked-strategy execution denial.
- Security/docs: separation of proposal, approval and activation duties.
- Definition of Done/gate: every active version has current evidence, owner and rollback.
- Rollback: suspend version, return to last approved strategy or zero trades.

## Source-of-truth map

- [Finance module](../../modules/finance.md)
- [Architecture and capabilities](architecture.md)
- [Risk, exits, compounding and modes](risk-and-trading-modes.md)
- [Strategy, backtesting and paper methodology](strategy-backtesting-paper.md)
- [Broker, execution and reconciliation](broker-execution-reconciliation.md)
- [Decision journal and observability](decision-journal-observability.md)
- [Testing strategy](testing-and-validation.md)
- [Market-data provider selection](market-data-provider-selection.md)
- [Threat model](../../security/finance-threat-model.md)
- [Backlog](../../BACKLOG.md#bigbrain-finance--policy-governed-autonomous-trading)
