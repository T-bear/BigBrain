# BB-081 robustness / out-of-sample foundation

## Metadata

- Date: 2026-08-12
- Baseline: `d2500473e4d2f1162060a953d66da82ddf732507`
- Scope: Finance BB-081
- Runtime outcome: implemented, persisted, deployed and restart-verified

## Status

The foundation is implemented. Current real evidence is methodology/engineering evidence and is classified `INSUFFICIENT_DATA`, not strategy validation.

## Evidence

## Exact plan and lineage

- Evaluation plan: `chronological-oos-walk-forward/v1`; robustness model `transparent-robustness-score-v2`; bounded grid `bounded-core-daily-v1`.
- Feature revision: `feature-a04bcf61e20a79ec`.
- Market revisions (16): `eodhd-23b42bae32d6d7de`, `eodhd-2973b33284f6f946`, `eodhd-417b87c7c9ff1096`, `eodhd-498026827fd5f895`, `eodhd-4d13774721c95b71`, `eodhd-53854b703d52c45f`, `eodhd-69668603dab5fdc5`, `eodhd-6a8d44394900aefd`, `eodhd-7cc9914336ecbc8a`, `eodhd-8a8b419115043082`, `eodhd-8e7a62bd62a5708b`, `eodhd-be3d30f62c19169b`, `eodhd-bf523d994e2a180a`, `eodhd-bfdfa7770fbebed0`, `eodhd-d7bfbf0bbf2dd1ec`, `eodhd-f2648baf57e65eb1`.
- Universe: SPY, QQQ, IWM, AAPL, MSFT, JPM, XOM and JNJ; 2025-08-11 through 2026-08-11; initial capital USD 100,000; seed 0.
- Existing BB-080 semantics remain: `daily-next-session-open-v1`, equal initial allocation, whole shares, no margin/short/leverage.

## Changes

Finance now owns versioned plans, chronological split/embargo, fixed expanding walk-forward, bounded parameter/cost diagnostics, decomposable score/sufficiency verdict, immutable SQLite revisions, read-only API/UI and provider-derived deletion lineage.

### Evaluation governance

The primary chronological split is 70/30 with a 50-session embargo matching the maximum materialized feature lookback. It produced 176 train and 26 test sessions. The evaluator also supports deterministic 60/40 and 80/20. Expanding walk-forward uses 126 initial train sessions, 25 test sessions and a 25-session step; three windows were possible. Minimum evidence requirements are 126 train sessions, 40 test sessions and three walk-forward windows. The 26-session test therefore forces `INSUFFICIENT_DATA` for every strategy.

SMA diagnostics use the available causal `core-daily-v1` neighborhood `(5,20)`, `(10,20)`, `(5,50)`, `(10,50)`; momentum uses periods 5, 10 and 20. This is a bounded sensitivity check, not optimization. Buy-and-hold has no parameter search. The cost ladder is zero, low (2 bps), base conservative (5 bps), high (10 bps) and stress (20 bps), with explicit per-share/minimum commissions. Maximum runs are bounded to 64 per evaluation.

### Real evaluation evidence

Seventy unique underlying deterministic backtest runs were referenced across the three evaluations (12 buy-and-hold, 30 SMA, 28 momentum), with nine walk-forward windows, seven parameter variants and fifteen cost points. The local build completed in 18,619 ms.

| Strategy | Evaluation / checksum | Train net | Test net | Test vs benchmark | Parameter sensitivity | Zero to stress net | Walk-forward positive | Verdict |
| --- | --- | ---: | ---: | ---: | --- | ---: | ---: | --- |
| buy-and-hold/v1 | `evaluation-976cf2e426a08c1f` / `sha256:a1dbf7fb4124bcab734a39cfdfddb542f1bc7b7bf92449f0d8385bc20c6f2712` | 16.07% | 6.42% | benchmark reference | not applicable | 28.96% → 28.75% | 0% benchmark-relative | `INSUFFICIENT_DATA` |
| sma-crossover/v1 10/20 | `evaluation-9ffc5e9a395651c5` / `sha256:a855a6c4c831c80c3cdd8664809cc8ae49c217008c2daa40bc8836e823059e74` | 5.62% | 5.36% | -1.05 pp | robust neighborhood; median 2.30%, range 1.33–5.36% | 17.25% → 14.50% | 66.7% | `INSUFFICIENT_DATA` |
| momentum/v1 20 | `evaluation-f944716303acfafa` / `sha256:2ee99325a8cd05070b6dcee3c3ea61280e9655a2ff958da9485675c79dc9593a` | 7.47% | 4.97% | -1.45 pp | robust neighborhood; median 3.37%, range 1.68–4.97% | 15.16% → 10.06% | 0% | `INSUFFICIENT_DATA` |

Higher configured friction monotonically reduced net return in deterministic tests and runtime evidence. Momentum degraded more than SMA or buy-and-hold across the ladder. Neither active reference strategy beat buy-and-hold in the primary test window. These are observations from a short engineering sample, not validation or advice.

## Security

Evaluation is offline/read-only. No credential or licensed payload enters API/UI evidence, and no broker, external order, PAPER, LIVE, optimizer or automatic strategy selection exists.

### Safety, persistence and verification

Golden tests cover chronological 60/40, 70/30 and 80/20 splits, embargo/no overlap, deterministic bounded grids, isolated-peak classification, cost monotonicity, sample insufficiency, future-feature invisibility, future-test mutation isolation and earlier walk-forward stability. Repeated builds produce the same run/evaluation IDs, metrics, verdicts and checksums; SQLite reopen/restart preserves exact detail. Read-only API exposes catalog and exact evaluation detail. Web shows train/test, sensitivity, cost degradation, walk-forward evidence, lineage, limitations and prominent `DATA INSUFFICIENT`, `RESEARCH` and “Ingen handel med riktiga pengar”.

EODHD preview/confirm deletion now inventories and removes covered robustness evaluations, windows, parameter/cost results, derived run references and indexes while protecting unrelated artifacts. Evaluation used existing local memory only. Provider accounting remained 16 attempts, 16 successes and zero failures/retries; BB-081 made zero provider requests.

The read-only current catalog exposes one current revision per strategy (three entries). Retention reports four immutable evaluation revisions because one superseded development revision was deliberately preserved instead of overwritten or deleted; it remains readable by exact ID and inside deletion lineage.

Limitations are persisted: current-survivor-selected eight-symbol universe, approximately one year, raw OHLC, incomplete corporate actions and full-liquidity next-open fills. No broker, external order, PAPER, LIVE, optimizer or automatic strategy selection exists.

## Remaining work

Accumulate materially longer historical memory or add a separately entitled zero-cost historical source, then rerun the immutable plan. Corporate-action-adjusted lineage is also valuable. Do not proceed to PAPER from this evidence.

## Resumption

Run `finance-robustness-build` through the documented local maintenance command, inspect the read-only catalog/detail endpoints and compare exact evaluation IDs/checksums. The command must remain provider-request-free.

## Sanitization

Detta är en sanerad GitHub-version. This report contains no licensed market rows, raw payload, secret, account identity, private address, sensitive filesystem path or profitability claim.
