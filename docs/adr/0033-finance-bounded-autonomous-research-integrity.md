# ADR 0033: Finance bounded autonomous research and integrity

- Status: Accepted for BB-092
- Date: 2026-08-22

## Context

Finance already owns immutable point-in-time feature revisions, deterministic next-open backtests, explicit hypothetical costs, chronological train/OOS separation with embargo, bounded parameter sensitivity and expanding-window robustness evidence. It needs a research loop without creating trading authority or an open-ended strategy language.

## Decision

BB-092 adds a code-reviewed `finance-research-signals-v1` allowlist over existing features, structured versioned hypotheses, deterministic families and immutable experiment evidence. One explicit API trigger runs at most three experiments and uses only existing allowlisted strategies and pinned market/feature revisions. An idempotency key identifies a run; hypothesis and experiment fingerprints prevent duplicate evidence inflation. Interrupted runs become failed audit evidence on restart.

`research-integrity-v1` fails closed on sample size, OOS/excess performance, walk-forward breadth, explicit cost assumptions, complete lineage, attempt accounting, bounded complexity and the existing robustness verdict. A passing result creates only a `CHALLENGER` research state. It cannot alter shadow strategies, Hard Risk policy, portfolio state, mode or execution authority.

DSR is `NOT_EVALUABLE` because v1 does not retain the required return-series moments and selection-population assumptions. PBO/CSCV is `NOT_EVALUABLE` because combinatorial partitions are absent. A negative control is deferred until a seeded label/permutation contract can be added without contaminating canonical observations. No substitute statistic or qualitative PBO label is emitted.

Research hypotheses currently reuse momentum20 and allowlisted fast/slow SMA relation evidence. Volatility20 and volume-ratio20 are registered research inputs but not autonomous variants. Macro evidence is not used: available Riksbank/ECB/current-history data is `REVISED_HISTORY_EXPLORATORY`, and causal FRED coverage is too narrow for this loop.

## Consequences

All attempts, including rejection and failure, remain queryable. Public-domain backup payloads retain related sanitized hypothesis/experiment metadata only when all referenced market/feature/robustness evidence is eligible; restricted provider raw history does not enter indefinite backup. Scheduler, governor, continuous operation, automatic champion promotion, PAPER, broker, orders, LIVE/AUTO and self-modifying code remain out of scope.
