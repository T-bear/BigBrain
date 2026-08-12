# BB-080 first deterministic real-data backtest engine

## Status and scope

Implemented, automatically verified, deployed and restart-verified on 2026-08-12. Finance remains read-only `RESEARCH`. Simulated fills are historical evidence objects, not orders. No broker, credential, PAPER, LIVE or external execution surface was added.

## Exact BB-078/079 evidence run

- Market revisions: `eodhd-23b42bae32d6d7de`, `eodhd-2973b33284f6f946`, `eodhd-4d13774721c95b71`, `eodhd-53854b703d52c45f`, `eodhd-6a8d44394900aefd`, `eodhd-8a8b419115043082`, `eodhd-8e7a62bd62a5708b`, `eodhd-bfdfa7770fbebed0`.
- Feature revision: `feature-5d0397a53d094a2f`.
- Coverage/universe: 251 sessions, 2025-08-11–2026-08-10; SPY, QQQ, IWM, AAPL, MSFT, JPM, XOM and JNJ; initial capital USD 100,000.
- Strategy versions: `buy-and-hold/v1`, `sma-crossover/v1` (10/20), `momentum/v1` (20).
- Simulation/sizing: `daily-next-session-open-v1` and `equal-initial-capital-whole-shares-v1`; seed 0.
- Costs: `zero-cost-v1`; `conservative-cost-v1` = USD 0.01/share, USD 1 minimum, 5 bps adverse slippage. These are research assumptions, not broker quotes.

| Strategy | Cost | Run ID | Final equity | Net return | Max DD | Trades | Benchmark/excess |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: |
| Buy and hold | zero | `backtest-e18c2f753c5a8bcf` | 129,278.22 | 29.278% | -5.042% | 8 | benchmark |
| SMA 10/20 | zero | `backtest-4ebff5b2d862a1fc` | 117,407.05 | 17.407% | -3.646% | 104 | 29.278% / -11.871 pp |
| Momentum 20 | zero | `backtest-513c8f13cf40abfe` | 115,321.47 | 15.321% | -3.555% | 194 | 29.278% / -13.957 pp |
| Buy and hold | conservative | `backtest-841ea81b62f3c1d9` | 129,221.04 | 29.221% | -5.044% | 8 | benchmark |
| SMA 10/20 | conservative | `backtest-6af7af4bc543857a` | 116,654.61 | 16.655% | -3.684% | 104 | 29.221% / -12.566 pp |
| Momentum 20 | conservative | `backtest-9ba01ef4945fc041` | 113,915.87 | 13.916% | -3.813% | 194 | 29.221% / -15.305 pp |

The six result checksums are recorded by the immutable catalog. Repeated execution returned the same IDs/checksums and `idempotent=True`. The first build read 42,168 feature rows, created 612 simulated fills and 14,166 stored events in 4,241 ms. It made no provider request; request accounting was eight before and after that command.

## Semantics and verification

Strategies receive only the completed bar, causally visible features and current simulated portfolio. Transition intents fill at the next available open; end-of-data intents remain unfilled. Whole-share allocation never borrows or creates negative cash. Journal rows retain market/feature references, intent, fill, cash/position transitions and mark-to-market. Curves retain cash, holdings, equity and drawdown; metrics include gross/net return, annualized return where valid, volatility, Sharpe-like daily/252 ratio, drawdown, exits, turnover, costs and benchmark/excess.

Golden tests prove next-open timing, future-feature masking, future-bar independence, same-day non-fill, cash/position/cost arithmetic, repeated signals, warmup, insufficient cash and missing next session. SQLite stores immutable runs, events, fills and curve points; read-only catalog/detail API and responsive Finance UI expose lineage, metrics, equity/drawdown and cost comparison.

EODHD deletion preview/confirmation now inventories dependent runs, journals, fills, curves, metrics and indexes before feature/market deletion. Generic strategy definitions remain.

## Runtime state note

Deployment occurred on the next UTC schedule date. The pre-existing acquisition worker—not the backtest command—performed its normal bounded daily ingestion independently, so runtime later gained newer immutable market/feature revisions and corresponding new run IDs. This does not mutate the six exact BB-078/079 runs above. The offline run was request-free; no provider call is required by or implemented inside the backtest engine.

## Limitations and next gate

Raw OHLC is not corporate-action adjusted, corporate-action coverage is incomplete, the universe is current-survivor-selected, fills assume full liquidity, and roughly one year is engineering evidence only. Results are not recommendations or profitability validation. The next safe slice is BB-081 robustness/out-of-sample and cost-sensitivity foundations; do not proceed directly to PAPER.

This sanitized report contains no licensed price rows, raw payload, secret, account identity, private address or sensitive filesystem path.
