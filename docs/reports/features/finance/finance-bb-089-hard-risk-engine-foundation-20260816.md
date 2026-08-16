# BB-089 M5 Hard Risk Engine foundation

## Metadata and status

- Date/baseline: 2026-08-16 / `7aa202bc88142399cd9a70085cec5bf4f23db16f`
- Policy: `research-eod-v1`
- Mode/budget: RESEARCH / 0 SEK
- Authority: no PAPER, broker, order, LIVE/AUTO or self-learning
- Deployment/runtime: API/Web deployed and restart-verified 2026-08-16
- Publication/CI: pending commit/push and GitHub Actions; no CI result is inferred

## Status

Foundation implemented, automatically verified, deployed and restart-verified. CI remains pending
publication. Sanitization notice: this report contains no secret, credential, private identity,
private address, raw provider payload, raw log or sensitive filesystem path.

Detta är en sanerad GitHub-version.

## Changes

Central policy/evaluator, immutable persistence, shadow linkage, durable halt/audit, read-only API/UI,
focused tests, ADR and lifecycle/security/roadmap documentation were added.

## Architecture and policy

The server-side `FinanceRiskEngine` is the authoritative deterministic policy evaluator below
Finance proposers. New shadow predictions retain their strategy signal and link a separate immutable
risk evaluation. Client verdicts, invalid mode, stale/missing evidence and invalid lineage fail
closed. IDs bind proposal and exact policy/strategy/parameter/source/feature/cutoff lineage. Policy
changes cannot rewrite history. ADR 0030 makes matching current risk evidence mandatory for any
future execution design; no execution contract was added.

Policy v1 uses hypothetical `ResearchCapital` 100,000 USD—not cash, account balance, buying power or
a real portfolio. Requested, allowed and risk-adjusted exposure are research evidence. Per-instrument
cap is 5%; requests above it reduce to the cap, and requests above 10% deny. Defaults were selected
as conservative research assumptions and were not tuned from returns.

## Rules and verdicts

`ALLOW` means hypothetical research passes; `REDUCE` caps exposure; `DENY` rejects unsafe/invalid
input; `HALT` blocks new hypothetical exposure; `INSUFFICIENT_DATA` identifies missing required
evidence. Every deny/halt has deterministic reason code and Swedish explanation.

Implemented gates: RESEARCH mode, no client verdict, canonical allowlist, positive price/previous
close, source and feature lineage, warmup, causal UTC/clock, provider/cadence health, CURRENT EOD
freshness, 15% abnormal move, 20-return population volatility capped at 8%, `volume.ratio.20` minimum
0.10, positive/request-cap sizing, 3% simulated daily loss, 10% rolling 20-session drawdown and three
consecutive losses. Friday remains fresh over weekend; the second completed weekday without a new
observation is stale. Spread is `NOT_EVALUABLE` because EODHD Free lacks reliable bid/ask. Sector
and aggregate portfolio/count/gross exposure are deferred because trustworthy metadata and a
coherent prospective portfolio do not exist. Market-open-now logic is intentionally absent.

Daily loss, drawdown and consecutive loss behavior is deterministic fixture simulation, not real
prospective P/L. Loss thresholds produce `HALT`. A durable system halt survives reopen, blocks
subsequent approval and requires an explicit audited recovery transition. Audit records previous/new
state, reason, policy, UTC time and evidence. No restart silently clears safety state.

## Shadow, API and UI

Only newly inserted predictions create linked risk evaluations. Existing BB-087 predictions are not
rewritten or retroactively marked approved. `TargetLong + DENY` remains two truthful facts. Read-only
API: `/api/v1/modules/finance/risk/status`, `/risk/policy`, `/risk/evaluations`, and
`/risk/evaluations/{id}`. There is no mutation endpoint for limits or halt and no order endpoint.
Finance UI keeps Market today → BigBrain now → Prospective result → Details & research, adds compact
Riskkontroll state and per-signal risk wording, then exposes technical evaluations under details.

## Persistence, recovery, retention and backup

SQLite risk evaluations are append-only/idempotent by deterministic evaluation/proposal identity.
Halt state and transition audit are durable. Source-linked risk evaluations contain minimized
derived provider evidence and inherit EODHD deletion scope; deletion removes them before lineage.
Pure halt-transition metadata may remain indefinitely. EODHD risk evidence is excluded from the
public-domain indefinite backup class.

## Security and adversarial evidence

Tests reject omitted/malformed evidence, invalid policy, fake instruments, client-supplied verdict,
stale EOD, invalid source/feature lineage, clock failure, invalid mode/price/exposure and duplicate
evaluation. Invariants prove maximum exposure, non-positive rejection, HALT/stale/lineage never
ALLOW, policy recording, signal preservation and deterministic idempotence. Public API is GET-only.
Future pentest scope now includes risk omission/tampering, forged/replayed ALLOW, stale reuse, mode
bypass, halt reset and client override.

## Runtime, tests and limitations

Focused backend: 15/15 passed. Full API: 423/423; Sentinel: 32/32; frontend: 111/111. Release build
passed with zero warnings/errors and frontend production build transformed 57 modules. Documentation
verification passed 157 Markdown files/89 unique BB IDs; Compose and `git diff --check` passed.
Redacted gitleaks worktree scan found 11 pre-existing worktree findings; the BB-089 staged/publication
scan is run after exact staging. Docker API/Web image builds and deployment passed.

Sunday runtime reported policy `research-eod-v1`, healthy/READY, no halt, zero evaluations and no
execution authority. Zero is correct: all 24 valid pending predictions predate activation and were
not retroactively evaluated. Journal remained 24 valid pending, zero outcomes and 24 invalidated
audit rows; latest session remained 2026-08-14. Cadence logged `no-provider-check`, with no provider
check/success timestamp and next action the weekday EOD window. API/Web were healthy after restart;
risk state remained unchanged. No fake session, prediction, evaluation or provider request occurred.
One bounded overview call transiently timed out while concurrent startup checks ran; subsequent
overview and component endpoints returned immediately, containers had zero restarts and logs showed
no failure. This was not reproducible after readiness.

Avanza MCP/unofficial public-data integration is documented only as `MANUAL_REVIEW / RIGHTS_UNKNOWN`.
MIT integration code does not establish underlying data rights. No call or integration was made.

## Future improvement proposals — NOT approved for implementation

- Trustworthy sector and bid/ask inputs plus their rights/retention model.
- A separately designed hypothetical portfolio for gross exposure and position-count gates.
- An authoritative exchange calendar if weekday/provider semantics prove insufficient.

Recommended next slice: Security/Penetration Testing Baseline while genuine prospective evidence
continues accumulating. This does not make PAPER eligible.

## Evidence

Evidence is the committed implementation, deterministic network-free tests and the exact dated
verification commands/results recorded above. Runtime and CI are separate evidence classes.

## Remaining work

The full Security/Penetration Testing Baseline, genuine multi-session prospective accumulation and
the explicitly deferred policy inputs remain. PAPER eligibility is not established.

## Resumption

Resume from the published BB-089 commit and current `origin/main`; verify risk status/evaluation
persistence and prospective counts before starting the separately scoped security baseline.
