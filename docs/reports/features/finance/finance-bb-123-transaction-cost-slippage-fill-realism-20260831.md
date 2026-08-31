# BB-123 — Complete BB-048 Transaction-Cost, Slippage & Fill Realism

Detta är en sanerad GitHub-version. No secret, credential, private address, account identifier,
raw market row, raw provider payload, raw log or sensitive filesystem path is published.

## Metadata

- Date: 2026-08-31
- Baseline/source of truth: `317e882ef90e15562642105ce3f7f5a2621002ad`
- Scope: deterministic historical research execution simulation only
- Finance boundary: `RESEARCH / 0 SEK / NONE`
- Result: **IMPLEMENTED / AUTOMATICALLY VERIFIED / DEPLOYED / BOUNDED RESEARCH VERIFIED**

## Status

BB-123 completes BB-048 for BigBrain's current daily-US-equity research evidence. It extends the
existing BB-080 engine and BB-081 cost ladder; no second backtester, broker/order contract, PAPER,
LIVE, AUTO or execution authority was introduced. No market data was acquired and no autonomous
research run was triggered.

## Evidence

### Existing capability inventory and gap matrix

| Requirement | Before BB-123 | BB-123 result |
|---|---|---|
| Next-session-open, causal signal timing | IMPLEMENTED BB-080 | Preserved; v2 requires the exact next calendar session |
| Whole shares, equal initial sizing, cash, no borrowing | IMPLEMENTED | Preserved and regression-tested |
| Per-share commission and minimum fee | IMPLEMENTED | Preserved in explicit v2 cost lineage |
| Fixed per-fill and proportional/notional fee | MISSING | IMPLEMENTED |
| Spread | MISSING | IMPLEMENTED as an assumed full spread, never observed quotes |
| Adverse fixed-bps slippage | IMPLEMENTED | Preserved, separated from spread and tested both directions |
| Fill price/fills/journal/equity/metrics | IMPLEMENTED | Extended with attempts, reasons and friction decomposition |
| Missing next bar/end of data | PARTIAL; next available bar/end remained implicit | Exact-next-session rejection and explicit end-of-data reason IMPLEMENTED |
| Invalid open/zero quantity/insufficient cash/no sale position | PARTIAL | Explicit deterministic rejection semantics IMPLEMENTED |
| Rejected/unfilled count | MISSING | IMPLEMENTED |
| Daily-volume participation/partial fills | NOT JUSTIFIED WITH CURRENT DATA | Explicitly deferred; daily volume is not intraday capacity evidence |
| FX | NOT JUSTIFIED IN CURRENT USD-only universe | Explicitly out of current contract |
| Corporate actions | KNOWN LIMITED | Unchanged raw-OHLC/incomplete-action limitation |
| Benchmark, gross/net, turnover, immutable replay | IMPLEMENTED | Preserved; spread cost added to detailed metrics |
| Cost sensitivity | IMPLEMENTED BB-081 | Existing five-step ladder upgraded, not duplicated |

### Execution-assumption contract

New runs use `daily-next-session-open-v2` plus
`next-session-open-full-fill/v2` and `equal-initial-capital-whole-shares-v1`. The immutable run
configuration fingerprints exact market/feature revisions, strategy/version/parameters, cost and
fill model versions, initial capital, universe, dates, sizing, seed and evaluation context.

`BacktestCostModel` records per-share commission, minimum commission, fixed per-fill commission,
proportional notional bps, assumed full-spread bps and adverse slippage bps. Fee semantics are:
fixed per-fill + max(minimum, per-share component when configured) + proportional fill-notional
component. All values must be non-negative.

Spread is a simulation assumption because daily OHLCV has no historical bid/ask quotes. A buy pays
half the assumed full spread above the reference open; a sell receives half below it. Slippage is
separate and always adverse: buy above/sell below the open. The production base assumption is USD
0.01/share, USD 1 minimum, 2 bps assumed full spread and 5 bps adverse slippage. These values are
transparent conservative research parameters, not broker quotes or calibrated historical spreads.

An intent is eligible only at the exact next `us-equities-ny-v1` session. Missing instrument bar,
invalid/non-positive open, zero quantity, insufficient cash, unavailable sell position, changed
contract state and end-of-data are explicit rejected/unfilled attempts. No later bar is silently
substituted. Successful attempts are full whole-share fills only.

Daily volume is carried into the research bar but no participation cap or partial fill is applied.
Daily aggregate volume cannot establish intraday liquidity, queue priority, order-book depth or the
tradable volume at the open; source volume may also have adjustment semantics different from raw
OHLC. A cap would create false precision. This limitation remains explicit.

### Persistence and compatibility

No schema migration was required. The existing immutable `result_json` stores the extended
configuration, fills, attempts and metrics. New assumptions produce new run IDs and checksums.
Legacy `daily-next-session-open-v1` runs remain unchanged and readable; absent v2 fields deserialize
as zero/null and are never reinterpreted as having spread or rejection evidence.

Deployment exposed a concurrent startup-worker/maintenance build race. `PersistBacktest` now uses
an atomic insert-or-ignore winner followed by exact checksum verification; equivalent builders
converge, while differing checksums still fail as immutable identity conflicts.

### Focused and affected regression

- Focused deterministic backtest/robustness/concurrency: 20/20 passed.
- Full API suite: 574/574 passed.
- Release solution build: passed, zero warnings/errors.
- Hand-calculated tests cover zero friction, per-share/minimum, fixed and proportional fees,
  adverse spread/slippage for buy and sell, combined friction, insufficient cash, invalid open,
  missing exact-next-session bar, end-of-data, whole shares, signal invariance, deterministic
  identity/replay, legacy JSON and concurrent persistence.

### Bounded scientific validation

The existing approved current feature/market lineage was evaluated offline through the normal
maintenance path: 265 sessions, eight instruments, 44,520 feature reads. No provider call,
strategy tuning or autonomous run occurred. Sanitized buy-and-hold aggregates demonstrate the
expected monotonic response:

| Assumption | Net return | Final equity | Total simulated friction | Fills |
|---|---:|---:|---:|---:|
| Zero | 29.65858% | 129,658.58 | 0.00 | 8 |
| Base v2 | 29.59159% | 129,591.59 | 66.98 | 8 |
| Stress v2 | 29.41012% | 129,410.12 | 248.46 | 8 |

For base v2, the 66.98 decomposes into 8.17 commission, 49.01 adverse slippage and 9.80 assumed
spread. The existing five-level ladder remained ranking-monotonic. The robustness verdict remains
`INSUFFICIENT_DATA`; this is simulator validation, not profitability evidence.

Runtime evidence grew from 797 to 878 immutable backtest runs and from 25 to 28 robustness
evaluations: exactly six bounded reference runs and 75 unique ladder/OOS runs plus three new v2
evaluations. Repeating both maintenance commands returned `idempotent=True`. Existing records were
not rewritten or deleted.

## Changes

- Extended the existing cost, fill, metrics and run-lineage records.
- Added exact-next-session fill/rejection processing and friction decomposition.
- Integrated assumed spread into the existing BB-081 five-level ladder.
- Made concurrent equivalent immutable persistence conflict-safe.
- Added focused scientific and concurrency regression tests.

## Security

Only the API service was rebuilt/recreated; existing volumes were retained. Runtime health is
healthy. Finance remained `RESEARCH`, broker disconnected, budget policy 0 SEK and execution
authority NONE. No credentials, provider payloads, raw rows or private runtime identifiers are
published.

## Remaining work

- Partial fills, market impact and volume participation remain scientifically unsupported by daily
  aggregate OHLCV and require separately qualified execution/liquidity evidence.
- FX remains outside the current single-currency US-equity simulation.
- Raw prices, incomplete corporate actions, current-survivor selection and short history remain
  research limitations.
- Backtest detail/catalog reads deserialize large immutable JSON histories and were slow during
  runtime inspection; profile this read path in a separate reliability task before expanding
  evidence volume further. It does not change calculated results.

BB-048 resulting status: **COMPLETE FOR CURRENT DAILY RESEARCH SCOPE / KNOWN DATA-BOUND
LIMITATIONS EXPLICIT**.

## Resumption

The recommended next scientific step is the owner's choice between stronger/longer evidence and
the remaining BB-049 governance work. Do not infer PAPER readiness or tune strategies from BB-123.
