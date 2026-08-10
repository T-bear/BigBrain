# BigBrain Finance – master roadmap

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

- Current phase/milestone: RESEARCH / M1 complete.
- Completed: M0 specification and M1 domain/evidence foundation with read-only module status.
- Active work: BB-071 waits for written provider confirmation.
- Next safe task: send the BB-071 provider inquiry, then evaluate the written response.
- Blocker for M2 implementation: BB-071. ADR 0021 was accepted by the product owner on
  2026-08-10, but acceptance authorizes no provider or ingestion.
- Safe parallel foundation: implement only provider-neutral entitlement/provenance types,
  fail-closed evaluation and synthetic invariants from BB-045. Provider payload ingestion,
  persistence and account work remain blocked.
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

### M2 – Historical market-data foundation — PLANNED

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

BB-046 research is complete. The provisional primary candidate is Twelve Data for
Nordic/global EOD, with Tiingo/Massive as US specialists. Public terms now show deletion
requirements after cancellation and leave parts of deterministic backtest/derived-data
use product-dependent. This does not start M2: BB-071 must establish an adequate written
entitlement first.

### M3 – Backtest engine — PLANNED

- Objective: deterministic, cost-aware portfolio and order simulation.
- Scope/tasks: event clock, fills, partial/rejected/delayed orders, fees, spread, slippage,
  FX/tax-fee assumptions, benchmarks, metrics and reproducible reports.
- Non-goals: claiming future profitability or using live execution.
- Prerequisites: M2.
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

### M5 – Hard Risk Engine — PLANNED

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
