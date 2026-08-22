# ADR 0033: Finance bounded autonomous research and integrity

- Status: Accepted for BB-092
- Date: 2026-08-22

## Context

Finance already owns immutable point-in-time feature revisions, deterministic next-open backtests, explicit hypothetical costs, chronological train/OOS separation with embargo, bounded parameter sensitivity and expanding-window robustness evidence. It needs a research loop without creating trading authority or an open-ended strategy language.

## Decision

BB-092 adds a code-reviewed `finance-research-signals-v1` allowlist over existing features, structured versioned hypotheses, deterministic families and immutable experiment evidence. One explicit API trigger runs at most three experiments and uses only existing allowlisted strategies and pinned market/feature revisions. An idempotency key identifies a run; hypothesis and experiment fingerprints prevent duplicate evidence inflation.

The remediation contract keeps the first row for an idempotency key immutable. Repeating a completed, running or recovered-failed key returns that same run; retrying work requires a new key. Startup reconstructs every stale `Pending`/`Running` row as a complete `FAILED` audit result with a recovery timestamp, explicit interruption reason and all already-linked experiments. It never deletes or overwrites the failed evidence. A partial unique SQLite index plus an atomic `BEGIN IMMEDIATE` start transaction permits exactly one global `Running` autonomous-research run. Initialization performs stale-run recovery before establishing that lease boundary.

`research_run_experiments` is the durable run-to-experiment audit relation. It supports deterministic experiment reuse across idempotently equivalent evidence while recording every run association; the immutable creator `run_id` remains on each experiment. Each experiment stores its actual evaluated parameter-variant `attempt_count`, and family multiplicity is the sum of these persisted values. Legacy rows are backfilled only when their own immutable complexity payload identifies the count; unknown legacy values remain unknown instead of being invented. The implemented target is next-session portfolio expectancy and therefore uses horizon 1.

Final remediation pins a research cycle to the exact evidence generation returned by `BuildRobustnessEvaluations`: its latest feature revision, normalized complete market-revision lineage, evaluation IDs and checksums. Persisted evaluation columns, immutable result payload and relational child evidence must agree. The enabled allowlist requires exactly one `momentum/v1` and one `sma-crossover/v1` evaluation before any experiment is created. The run may then apply its experiment budget to this coherent ordered set. Historical evaluation ordering and timestamps never choose evidence; missing, conflicting, incomplete or lineage-mismatched current evidence fails the durable run with a controlled current-evidence reason and never falls back to an older generation.

`research-integrity-v1` fails closed on sample size, OOS/excess performance, walk-forward breadth, explicit cost assumptions, complete lineage, attempt accounting, bounded complexity and the existing robustness verdict. A passing result creates only a `CHALLENGER` research state. It cannot alter shadow strategies, Hard Risk policy, portfolio state, mode or execution authority.

DSR is `NOT_EVALUABLE` because v1 does not retain the required return-series moments and selection-population assumptions. PBO/CSCV is `NOT_EVALUABLE` because combinatorial partitions are absent. A negative control is deferred until a seeded label/permutation contract can be added without contaminating canonical observations. No substitute statistic or qualitative PBO label is emitted.

Research hypotheses currently reuse momentum20 and allowlisted fast/slow SMA relation evidence. Volatility20 and volume-ratio20 are registered research inputs but not autonomous variants. Macro evidence is not used: available Riksbank/ECB/current-history data is `REVISED_HISTORY_EXPLORATORY`, and causal FRED coverage is too narrow for this loop.

## Consequences

All attempts, including rejection and failure, remain queryable through bounded, deterministically ordered run and experiment catalogs plus detail endpoints. `REJECTED`, `INCONCLUSIVE`, `NOT_EVALUABLE`, `PROMISING` and `CHALLENGER` are mutually exclusive counts globally and per run. Public-domain backup payloads retain related sanitized hypothesis/experiment/run-link metadata only when every experiment linked to a run is eligible; restricted provider raw history does not enter indefinite backup. Scheduler, governor, continuous operation, automatic champion promotion, PAPER, broker, orders, LIVE/AUTO and self-modifying code remain out of scope.
