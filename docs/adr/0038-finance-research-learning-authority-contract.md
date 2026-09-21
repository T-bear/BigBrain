# ADR 0038: Finance Research Learning proposal and evidence authority

- Status: Accepted
- Date: 2026-09-21
- Checkpoint: BB-131A — accepted documentation-only architecture
- Baseline: `7747b8f715204dc3ccf753f170238b40238d1b11`
- Acceptance: explicit owner/architect approval of candidate `1eb50979a7923f115b4b182fbee15de735ea1f7e`; no implementation authorization

## Context

Finance already has immutable data/features, deterministic backtests, BB-124 holdout governance,
BB-092 bounded research, BB-129 campaigns and independent Hard Risk authority. A future reasoner
could generate hypotheses adaptively; the current fixed-population controls and exact-lineage
holdout lookup do not by themselves govern an unbounded adaptive loop. A second engine or an
AI-authored scientific verdict would bypass accepted responsibilities.

## Decision

**AI MAY PROPOSE. DATA MUST PROVE. RISK MAY VETO.** Finance owns typed proposal admission,
identity, eligibility, budgets, exposure history, existing deterministic engines and results.
The reasoner receives only an authorized bounded evidence projection and returns one structured
suggestion or no useful proposal. Free-form explanations are untrusted metadata. Existing
ResearchHypothesis and scientific result vocabulary are reused, not replaced.

[The accepted architecture contract](../architecture/finance/research-learning-contract.md) defines frozen
exact input/plan identities, family/program budgets that cannot reset through wording or new
keys, all-trial/negative history and single-use protected holdout. Existing results remain
immutable. Missing evidence remains missing; limited private research entitlement does not
become permission to export evidence to an AI provider. Statistical significance is not claimed.

Brain remains orchestration only through normal authorized Finance contracts; no direct database,
shell, arbitrary HTTP, provider or execution capability. Existing research remains independent of
reasoner availability. Real model integration and production adaptive iteration require separate
security, rights, admission/recovery and scientific review. No PAPER/LIVE/AUTO/broker/orders/capital
capability is introduced or implied by research success.

The proposed first implementation is a synthetic fake-reasoner contract/replay fixture using
existing engines and isolated existing SQLite result persistence. No runtime API/worker/provider
or production journal/schema is part of that slice. It proves engineering boundaries, not unseen
real-market holdout, adaptive statistical validity or production crash atomicity.

## Alternatives

- Direct model access to Finance API/database or full robustness results: rejected as the
  proposed design because it leaks protected evidence and bypasses admission.
- New AI backtester, result taxonomy or experiment platform: rejected; existing Finance owns it.
- Treat BB-092/129 limits as complete adaptive-search protection: rejected; accounting units,
  exposure scopes and actual execution support differ.
- Real AI provider first: deferred until the fixture and independent security/rights review.

## Consequences and status limits

A new proposal envelope and future atomic admission/exposure journal are justified responsibilities,
not implemented capabilities. Current calculations, dataset identities, policy versions and
historical evidence are untouched. Source rights may limit retention/replay; no indefinite retention
exception is created. This ADR is accepted alongside ADR 0021–0025/0030/0033–0036;
it does not silently accept proposed trading ADRs 0017–0020 or authorize any next checkpoint.
Finance remains **RESEARCH / 0 SEK / NONE**. No deployment.
