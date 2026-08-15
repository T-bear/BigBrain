# ADR 0029: Finance prospective predictions are immutable knowledge-time evidence

- Status: Accepted
- Date: 2026-08-15
- Decision owner: Product owner

## Context

Historical replay can answer what a strategy would have done, but cannot prove what Finance knew before a later outcome. Current EOD observations, features and strategy versions already have immutable lineage, while the earlier shadow prototype was synthetic and process-local.

## Decision

Prospective Finance evidence is a separate, append-oriented journal. A prediction pins instrument, session, provider/source revision, observation knowledge time, knowledge cutoff, feature revision, strategy/version, parameter fingerprint and a versioned next-source-session horizon. Its deterministic identity suppresses retries. Only observations and features knowable at the cutoff may contribute.

The original prediction is immutable. A later eligible source observation creates a separate outcome record and advances only evaluation state. A restart may recover missing work only while the horizon is not already knowable; historical backfill never becomes prospective evidence. Clock-integrity, source entitlement, current-session age and warmup gates fail closed.

All records are `RESEARCH`. A shadow signal is not an order and no broker, portfolio-execution or self-modification path exists. EODHD-derived journal evidence remains source-retention-dependent and is not admitted to public-domain indefinite backups.

## Consequences

- Historical backtests and prospective scorecards remain separate.
- Small samples are labelled bootstrapping rather than profitable or validated.
- Outcome evidence can be appended without rewriting the prior belief.
- Corrected source data remains additional lineage; it cannot cosmetically rewrite the prediction.
- PAPER and LIVE remain separately gated by risk, prospective evidence, security and explicit product-owner authorization.
