# BB-084 public historical dataset research

## Metadata

- Date: 2026-08-15
- Baseline: `d9eadbb3f3533e70e57cd12ad6890b06e36e4772`
- Scope: WIKI and Zenodo dataset rights, provenance and artifact identity

## Status

Research complete: WIKI accepted for bounded promotion; Zenodo requires manual review.

## Evidence

## Scope and sources

Research was retrieved 2026-08-15. Nasdaq Data Link's first-party usage documentation identifies
database 4922/WIKI as EOD prices, dividends and splits released into the public domain:
<https://docs.data.nasdaq.com/v1.0/docs/in-depth-usage>. The evaluated mirror was
<https://github.com/kmfranz/trading_pairs>, commit `aff4c4f3b677b0434bfedbc12b4137facaf7a0bb`.
Its LFS pointer declared 235,562,224 bytes and SHA-256
`dd5127aae478d270150904fcbad6e96a42e461e13c3d48a1587edb9b89cea43e`; the normal Git LFS
download matched both exactly.

The file has 14 documented WIKI fields: ticker/date, raw OHLCV, ex-dividend, split ratio and
adjusted OHLCV. It contains 2,155,310 accepted rows, 3,186 tickers and coverage
2014-01-02–2016-12-19. Another 11,295 malformed/impossible OHLCV rows were rejected (0.521%).
This is a credible 2016 WIKI snapshot, not the later final 2018 release. Survivorship metadata
is unknown. No date overlap exists with current EODHD 2025–2026 memory, so comparison is
`INSUFFICIENT_OVERLAP`, not a consistency claim. Decision: pass and bounded promotion.

Zenodo DOI <https://doi.org/10.5281/zenodo.20192822> was authored by Mohammad Al Ridhawi,
Mahtab Haj Ali and Hussein Al Osman, University of Ottawa, version 1.0.0, created/modified
2026-05-15. Its 4,084,638-byte ZIP matched published MD5
`4c8e9f2ad7be1575bd533cb2f0b9eee6`; SHA-256 is
`214fa7d92464cf657d30a0df7c5e5a40eb334db20f73db0b90d80ce717a11180`.
It contains 21 daily raw/adjusted OHLCV series, 207,158 rows, 1982-01-04–2025-03-28.
CC BY 4.0 and attribution apply to the authors' curation. The authors state the observations
were retrieved from Yahoo Finance via yfinance 1.3.0; the deposit does not establish underlying
redistribution rights. Decision: `MANUAL_REVIEW_REQUIRED`, no promotion.

No additional candidate survey was needed. No paid source, account, CAPTCHA, browser automation,
access-control bypass or mirror code execution was used. External acquisition requests were one
Git clone/LFS artifact acquisition and one Zenodo static file download (with metadata/license
research reads); the large WIKI artifact was downloaded once and reused locally.

## Changes

The research results are encoded by ADR 0027, the intake architecture and runtime manifests.

## Security

No downloaded code was run; raw rows, secrets and private runtime paths are excluded here.

## Remaining work

Underlying Yahoo redistribution provenance needs authoritative clarification before Zenodo could
leave manual review.

## Resumption

Start from the two candidate IDs and immutable hashes in this report; do not redownload WIKI.

## Sanitization

Detta är en sanerad GitHub-version. It contains public source URLs and aggregate evidence only.
