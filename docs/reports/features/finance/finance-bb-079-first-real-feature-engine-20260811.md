# BB-079 first real feature / indicator engine

## Metadata

- Title: BB-079 first real feature / indicator engine
- Date: 2026-08-11
- Baseline: `6a6fda6f18fd3b2f9eb57ee241c3fd549bac9ccf`
- Feature set: `core-daily-v1`
- Engine: `daily-feature-engine-v1`
- Runtime outcome: implemented, persisted, deployed and restart-verified
- Related commit: assigned on publication

## Status

BigBrain Finance now derives its first provider-neutral immutable feature revision from the
existing BB-078 real local memory. The build made no market-data request; EODHD accounting
remained exactly eight requests. Finance remains `RESEARCH` and the output is measurement,
not a trading signal.

## Evidence

The input is the eight immutable EODHD market revisions for SPY, QQQ, IWM, AAPL, MSFT, JPM,
XOM and JNJ, containing 2,008 daily observations over 2025-08-11 through 2026-08-10. Source
revision IDs are `eodhd-23b42bae32d6d7de`, `eodhd-2973b33284f6f946`,
`eodhd-4d13774721c95b71`, `eodhd-53854b703d52c45f`,
`eodhd-6a8d44394900aefd`, `eodhd-8a8b419115043082`,
`eodhd-8e7a62bd62a5708b` and `eodhd-bfdfa7770fbebed0`.

The resulting revision is `feature-5d0397a53d094a2f`, checksum
`sha256:5d0397a53d094a2f898942428b8fdfb8c0ee367deed8b69d29739b572d377846`.
It contains 42,168 values: 39,616 available and 2,552 explicit warmup values, with zero
reported quality issues. The measured first build took 472 ms; an immediate repeat returned
the same revision/checksum and `idempotent=True`.

## Changes

`core-daily-v1` contains simple 1/5/20-period returns, one-period log return, SMA and EMA
5/10/20/50, momentum 5/10/20, non-annualized population return volatility 10/20, Wilder
RSI14, Wilder ATR14, average volume 20 and volume ratio 20. Statistical floating-point
results use the runtime's IEEE-754 calculation and are normalized to 12 decimal places
before persistence and fingerprinting.

Price features use raw close/OHLC only; volume uses the provider's separately classified
split-adjusted volume. Adjusted close is not mixed into this feature set. Full corporate-
action adjustment and exchange-calendar completeness remain future work.

Definitions and values record version/fingerprint, required inputs/lookback, calculation,
warmup/missing/gap behavior, source revision/range, knowledge time, engine version and
quality. SQLite stores immutable revision metadata and indexed values. The bounded read-only
API exposes definitions, revision summary, latest values and instrument history; optional
UTC `knowledgeAsOfUtc` hides revisions and values not yet known at the replay boundary.

## Determinism and temporal safety

Known-answer tests cover every formula. Repeating identical market revision, definitions,
parameters and engine produces identical ordering, values and checksum. A correction/source
revision change creates a different feature revision while the original remains readable.
Explicit future-horizon tests prove that a value at T is unchanged when later observations
are appended. Knowledge time is the maximum causal input time; unavailable/warmup values are
never backfilled, zero-filled or made visible early.

## Persistence, API and UI

After API/Web recreation, the API reported `core-daily-v1`, the exact feature revision,
eight source revisions, durable persistence and the same counts/checksum. The existing 2,008
REAL EOD observations remained. A second restart plus local rebuild remained idempotent and
made no provider request.

The deployed responsive UI displayed SMA20, EMA20, RSI14, ATR14, 20-day volatility,
20-day momentum and 20-day volume ratio with feature/source revision and quality state. Both
390x844 and 1440x1000 checks retained `RESEARCH`, `Ingen handel med riktiga pengar`, honest
REAL EOD labeling, no trading controls, no overflow, no console errors and no external
browser requests.

## Retention lineage

The EODHD retention projection now inventories 42,168 dependent feature values and one
feature revision. Expiry blocks derived processing; deletion preview includes feature
values/revisions and confirmed scoped deletion removes them before their market inputs.
Generic definitions may remain because they contain no provider observations. Runtime
retention is active; no real deletion preview or destructive action was run.

## Verification

- Focused Finance backend: 20/20 passed; formula, edge, no-lookahead, lineage, persistence,
  API and retention fixture coverage included.
- Full backend: 369 API + 32 Sentinel = 401/401 passed; full Web: 107/107 passed.
- .NET Release and Web production builds passed with no .NET warning/error.
- Documentation: 136 Markdown files and 79 unique BB IDs; Compose validation and
  `git diff --check` passed.
- Runtime: API/Web healthy; feature API and UI passed; market request count remained 8.

## Security

Feature computation is fully local and adds no paid service. No provider token, environment
dump, provider payload or sensitive header was read or published. There is no broker,
order, PAPER, LIVE, LIMITED_AUTO or AUTO capability.

## Remaining work

There is no adjusted/corporate-action feature set, full exchange-calendar gap certification,
strategy, portfolio, fill/cost simulator or live source. The next safe slice is M3's minimal
deterministic research backtest foundation binding exact market and feature revisions; it
must remain offline/read-only and may not introduce orders or paper trading.

## Resumption

Build locally with `finance-features-build`; inspect through
`GET /api/v1/modules/finance/features` using bounded instrument/date/limit filters. Always
retain exact market and feature revision IDs in future evaluation evidence.

## Sanitization

Detta är en sanerad GitHub-version. It contains aggregate counts, public symbols, revision
IDs and checksums, but no secret, licensed raw payload, private address or resolved runtime
environment.
