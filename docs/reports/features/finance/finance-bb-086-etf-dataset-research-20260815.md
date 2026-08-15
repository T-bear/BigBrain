# BB-086 legitimate zero-cost ETF dataset research

## Metadata

- Date/retrieval date: 2026-08-15
- Baseline: `aee00db93614e9183a2aa56d87fabf53db482a50`
- Target: daily EOD/OHLCV history for SPY, QQQ and IWM
- Budget/mode: 0 SEK, `RESEARCH`
- Result: no candidate acquired; all decisions fail closed

## Status

Research complete. No candidate passed the mandatory rights/provenance and supported-access gates.

## Evidence

## Authoritative instrument identity

State Street identifies SPY inception as 1993-01-22, Invesco identifies QQQ inception as
1999-03-10, and iShares identifies IWM inception as 2000-05-22. Candidate dates before those
boundaries would be invalid. Sources:

- <https://www.ssga.com/us/en/individual/etfs/state-street-spdr-sp-500-etf-trust-spy>
- <https://www.invesco.com/qqq-etf/en/home.html>
- <https://www.ishares.com/us/products/239710/IWM>

## Existing WIKI inspection

The retained `WIKI_PRICES.csv` was reused read-only: 235,562,224 bytes, SHA-256
`dd5127aae478d270150904fcbad6e96a42e461e13c3d48a1587edb9b89cea43e`, documented 14-column
raw/adjusted WIKI schema. An exact ticker scan returned zero SPY, QQQ and IWM rows. Decision:
`REJECT` for BB-086 coverage; no new request or artifact.

## Bounded candidate matrix

| Candidate / URL | Host/publisher and underlying source | License/rights evidence | Coverage/schema/size/update evidence | Decision |
| --- | --- | --- | --- | --- |
| Existing WIKI snapshot / <https://github.com/kmfranz/trading_pairs> | GitHub mirror of Nasdaq/Quandl WIKI public-domain data | First-party Nasdaq public-domain evidence already accepted by BB-084 | Actual artifact above; zero target rows | `REJECT`: no ETF coverage |
| Stooq official daily CSV / <https://stooq.com/q/d/l/?s=spy.us&i=d> | Stooq; underlying exchange-data lineage and adjustment semantics remain undocumented | ADR 0022 allows bounded owner-accepted personal research only if technical access passes; no redistribution claim | Intended daily OHLCV/long history; one normal SPY GET returned 796-byte HTML browser proof-of-work, SHA-256 `605ae25f670c3df601bfccc6bfd646ff2b8f5e01876d9699325a659c430a80c9`, not CSV | `REJECT`: supported acquisition blocked; no challenge bypass |
| HF Data Library / <https://doi.org/10.5281/zenodo.19501605> | Ahmed Elkassabgi/UCA; pre-March-2022 PiTrading consolidated tape, later IEX-only | Deposit/site says CC BY 4.0; IEX terms permit distribution with attribution, but no evidence clears redistribution of the paid/proprietary PiTrading/full-tape portion | SPY/QQQ and likely IWM; minute plus daily aggregates, split/dividend adjusted, mostly 2002–present; fixed 2022–23 universe; free account/API key required | `MANUAL_REVIEW`: mixed-source rights and source break unresolved; no credential fabricated |
| LambdaClass `data-v1` / <https://github.com/lambdaclass/options_portfolio_backtester/releases/tag/data-v1> | LambdaClass mirror; vanished `philippdubach/options-data`; upstream sourcing explicitly undocumented | Repository code is MIT, but `DATA_NOTICE.md` asserts no license over data and limits posture to research reproducibility | SPY/IWM 2008–2025, QQQ 2011–2025; daily Parquet OHLCV; source-pinned hashes published (for example SPY `847e60a4…79db`) | `MANUAL_REVIEW`: mirror identity is strong but underlying rights/provenance fail |
| Kaggle SPY intraday / <https://www.kaggle.com/datasets/abidou/spy-intraday-ohlc> | Individual uploader; underlying source not established | Uploader marks CC0; uploader ownership of underlying quotations is unproved | SPY intraday OHLC, about 13 MB; exact dates/volume/adjustments not authoritative | `MANUAL_REVIEW`: CC0 cannot launder unknown source rights |
| Kaggle cleaned SPY/QQQ minute / <https://www.kaggle.com/datasets/cesarecastro/cleaned-spy-and-qqq-1-minute-data> | Individual uploader; no dataset description or underlying provenance | Uploader marks CC0 only | SPY/QQQ minute data; no credible date/schema/adjustment/version statement; no IWM | `MANUAL_REVIEW`: provenance and semantics unknown |
| Sakarya multi-asset study / <https://dergipark.org.tr/en/download/article-file/5427591> | Cemal Öztürk/Sakarya University journal; Yahoo Finance via yfinance | Article availability is not a data redistribution license; Yahoo underlying rights are not established | Reported daily OHLCV: SPY/QQQ 2000-01-03–2025-10-30 (6,497 each), IWM 2000-05-26–2025-10-30 (6,396); backfill interpolation and indicators; no authoritative raw artifact | `REJECT`: no compatible raw artifact/license and transformed Yahoo provenance |
| Nasdaq Data Link ETFG/FUND / <https://docs.data.nasdaq.com/docs/in-depth-usage-1> | ETF Global table hosted by Nasdaq Data Link | Dataset-specific rights/product access; platform documentation is not a public-domain grant | Example includes SPY/IWM tickers but describes fund-table queries, not historical OHLCV; QQQ/coverage/files/checksum not established | `REJECT`: wrong product shape and no qualifying rights evidence |

No downloaded dataset exists for the seven external candidates, so artifact filename, byte size,
retrieval hash, archive contents and schema fingerprint are correctly `not applicable`. Published
mirror hashes are identity leads only, not BigBrain acquisition evidence.

## Decision and stop condition

Acquisitions and successful market-data responses: 0. Public research reads were bounded to the
evidence above; Stooq market-data probes: 1, returning verification HTML. No account, API key,
CAPTCHA/proof-of-work solution, browser
automation, paywall bypass, repository code execution or paid service was used.

Because nothing was classified `ACQUIRE`, BB-084 correctly created no new candidate/artifact,
validation or promotion record. Consequently there are zero new observations, coverage ranges,
overlap metrics, price-basis claims, revisions or backups. This is a successful legal/scientific
fail-closed result, not an assertion that the candidate prices are inaccurate.

## Historical-bootstrap assessment

Current WIKI evidence adds five equities across 2014–2016, while current EODHD supplies recent
SPY/QQQ/IWM evidence. It does not provide ETF evidence for the dot-com era, 2008 crisis, full
post-2009 expansion, 2020 crash/recovery or 2022 tightening in one legitimate immutable source.
Historical ETF bootstrap is therefore incomplete. Repeating mirror searches has diminishing value;
new acquisition should resume only for a named first-party/open artifact with explicit underlying
rights or a separately approved entitlement.

## Changes

Research, current-state, roadmap and report-catalog documentation changed. No executable or runtime
artifact changed.

## Security

No access control was bypassed and no downloaded code or untrusted market artifact was executed.

## Remaining work

SPY/QQQ/IWM multi-regime historical evidence remains unavailable under a qualifying zero-cost
source. Resume only from a newly evidenced source rather than repeating the same mirrors.

## Resumption

Use this candidate matrix, BB-084 quarantine and BB-085 source-tagged backup rules. Revalidate
dated terms before any future acquisition.

## Sanitization

Detta är en sanerad GitHub-version. It contains public URLs, aggregate facts and hashes only. No
raw market rows, credential, account identity, private address/path or challenge solution is
published.
