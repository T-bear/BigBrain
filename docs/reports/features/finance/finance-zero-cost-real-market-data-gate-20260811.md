# Finance zero-cost real market-data gate

## Metadata

- Date checked: 2026-08-11
- Scope: BB-075 current zero-cost source sweep and activation gate
- Budget: **0 SEK** for external Finance market data until the product owner changes it
- Result: **CASE E — FAIL CLOSED; NO ZERO-COST SOURCE AUTHORIZED**
- Runtime data result: no account, key, provider request, payload or real observation
- Related commit: assigned on publication

## Status

No investigated zero-cost source passed BigBrain's exact combined gate for authorized
automation, local raw/normalized storage, retention, deterministic replay, backtesting,
derived evidence and the relevant termination/exchange scope. No adapter or persistence was
implemented and no provider was activated. Finance remains RESEARCH with zero real data.

Twelve Data Personal remains an entitlement-cleared paid fallback but is inactive because
the current external market-data budget is exactly 0 SEK. Twelve Data Basic is not an
operational option.

## Evidence

Current first-party product, pricing, API and legal material was retrieved or rechecked on
2026-08-11. “Publicly reachable”, “free”, “personal use”, a downloadable CSV or an
open-source client was never treated as a storage/backtesting grant. Unknown rights fail
closed.

## Changes

- Published the fresh provider matrix and exact unresolved questions.
- Recorded BB-075 and the hard 0-SEK product constraint in canonical Finance documents.
- Corrected the existing fail-closed observation read model so it reports the current
  `ZERO-COST ENTITLEMENT GATE`, not the superseded `BB-071 / STATE B` wording.
- Added no external adapter, provider policy, account setup, credential, production market
  memory or trading capability.

## Exact gate

The target is private, self-hosted, personal, non-commercial, non-redistributed US
equity/ETF observation and research. An adequate source must explicitly cover the exact
product/tier/feed plus automated acquisition, local raw and normalized storage, required
retention, replay/backtesting, derived/shadow/audit artifacts, own-funds use and applicable
exchange/termination duties at 0 SEK.

## Provider decisions

| Source/product | Cost and technical scope | Rights evidence | Decision |
| --- | --- | --- | --- |
| Alpaca Basic / free IEX | $0; account/API key; IEX realtime; 30 streamed symbols; US stocks/ETFs; historical since 2016; 200 historical calls/min; latest 15 minutes restricted | Personal/non-commercial use is stated and reproduction/distribution/commercial exploitation is restricted. Public material does not explicitly grant local raw/normalized retention, backups/revisions, post-account retention, derived/shadow/audit retention or the full IEX-specific lifecycle. Brokerage-linked account is required. | **HUMAN CONFIRMATION REQUIRED** |
| Stooq official downloads | $0 public chart/download surfaces; long US history is technically visible; pandas-datareader is a client path | No sufficiently explicit official grant was located for automated acquisition, durable raw/normalized storage, replay/backtesting, derived retention, post-use retention or upstream venue restrictions. Client library licensing is not data entitlement. | **INSUFFICIENT EVIDENCE** |
| Yahoo Finance / yfinance | $0 unofficial client; historical quotes and current/WebSocket features are technically exposed | Yahoo's current general terms prohibit automated collection without express prior permission. `yfinance` is not affiliated with Yahoo and explicitly delegates data rights to Yahoo terms; its Apache license covers code, not Yahoo data. Durable caching/retention is not granted. | **DENIED / INCOMPATIBLE** |
| Nasdaq Data Link free/open catalog | Free account/key; REST, Python and R support; many free/open macro/public datasets, commonly daily with lag | Free/open datasets have dataset-specific rights. No qualifying current free US equity/ETF OHLCV dataset with the complete required lifecycle was identified; most relevant market datasets are premium. Platform access is not a dataset license. | **INSUFFICIENT EVIDENCE / NO QUALIFYING DATASET** |
| EODHD Free Starter | $0, no card; account/key; 20 calls/day and 20/min; past year; limited EOD/splits/dividends and limited live surface | Terms permit non-professional personal storage/manipulation/analysis only during an active subscription and require deletion of all copies within one month after termination/expiry. Exact deterministic replay/backtest, normalized/backup/revision, derived/shadow/audit deletion scope and free-product exchange scope remain insufficiently explicit. | **HUMAN CONFIRMATION REQUIRED** |
| Alpha Vantage free key | $0; 25 requests/day; free daily history; realtime and 15-minute-delayed US data are premium-only | Current terms classify investment analysis, research, testing and monitoring beyond simple personal usage as commercial use. Storage/retention and exact derived lifecycle are not granted for this target. | **DENIED / INCOMPATIBLE** |
| Finnhub free | $0; account/token; pricing states personal use and 60 calls/min; free WebSocket up to 50 symbols; free OHLC history is not listed | Public product material establishes technical/personal access but not local raw/normalized storage, retention/termination, replay/backtest, derived/shadow/audit or exact exchange rights. | **INSUFFICIENT EVIDENCE** |
| Financial Modeling Prep Basic | $0; 250 calls/day; EOD and up to five years shown | Personal use exists, but terms say content may not be copied/downloaded without prior written approval and restrict derivative works. That conflicts with BigBrain memory without separate approval. | **HUMAN CONFIRMATION REQUIRED / INCOMPATIBLE BY DEFAULT** |
| Direct IEX/exchange/public-regulatory sources | No adequate zero-cost operational product identified | Exchange data products carry product/agreement conditions; no first-party free endpoint plus complete storage/research/retention grant for the watchlist was found. SEC/EDGAR is authoritative for filings, not OHLCV market memory. | **NO QUALIFYING SOURCE** |

## Source details and termination consequences

### Alpaca Basic / IEX

Official documentation confirms zero price, US stocks/ETFs, IEX-only realtime coverage,
30 WebSocket symbols, history since 2016 and 200 calls/minute. The Terms and Customer
Agreement support personal/non-commercial consumption and prohibit reproduction,
distribution, sale or commercial exploitation without consent. They do not map BigBrain's
stored raw, normalized, backup, revision, replay, derived, audit and post-termination
classes. The existing unsent Alpaca inquiry remains the smallest safe next action.

### Stooq and pandas-datareader

Stooq exposes official charts/downloads and identifies upstream suppliers. No official
license text found in this sweep grants the complete automated local-memory lifecycle.
`pandas-datareader` merely automates access; its software license cannot supply missing
Stooq or upstream market-data rights.

### Yahoo Finance and yfinance

Yahoo prohibits automated collection from its services without express prior permission.
`yfinance` documents itself as unofficial/unvetted and says users must consult Yahoo's
terms for actual-data rights. WebSocket support and local caching features are technical
capabilities only. This source is not eligible for BigBrain production ingestion.

### Nasdaq Data Link

Nasdaq Data Link supports free accounts, REST, Python and R and describes a broad free/open
catalog, chiefly public and macroeconomic datasets. Dataset rights are specific and most
market datasets are premium. No current exact free US equity/ETF OHLCV product satisfying
the watchlist and entitlement matrix was identified.

### EODHD Free Starter

Free Starter is genuinely 0 SEK with no card, 20 calls/day, one year of EOD data and
limited splits/dividends/live access. Terms expressly permit private storage/manipulation/
analysis during an active subscription, but require deletion of every copy within one month
after termination. Because exact replay/backtest and derived/audit artifact scope is not
mapped, BigBrain does not activate it on interpretation alone.

### Alpha Vantage and Finnhub

Alpha Vantage's current license text explicitly places investment analysis, research,
testing and monitoring outside its ordinary personal-use grant; current/delayed US equity
data is also premium. Finnhub advertises a free personal tier and realtime technical access,
but its public evidence does not close BigBrain's storage/retention/replay lifecycle.

## First-party sources

- Alpaca: `https://docs.alpaca.markets/docs/about-market-data-api`,
  `https://files.alpaca.markets/disclosures/library/TermsAndConditions.pdf`, customer agreement
- Stooq: `https://stooq.com/` official quote/download surface and linked terms
- Yahoo: `https://legal.yahoo.com/xw/en/yahoo/terms/otos/index.html`,
  `https://github.com/ranaroussi/yfinance`
- Nasdaq Data Link: `https://docs.data.nasdaq.com/docs/getting-started`, tables API documentation
- EODHD: `https://eodhd.com/financial-apis/quick-start-with-our-financial-data-apis`,
  `https://eodhd.com/financial-apis/terms-conditions`
- Alpha Vantage: `https://www.alphavantage.co/terms_of_service/`, support/pricing pages
- Finnhub: `https://finnhub.io/pricing`, registration and API documentation
- FMP: `https://site.financialmodelingprep.com/pricing-plans`,
  `https://site.financialmodelingprep.com/terms-of-service`

These observations are dated and product terms can change. Private/provider clarification
must be retained privately and represented in Git only as a sanitized decision.

## Security

Detta är en sanerad GitHub-version. It contains no account identity, API key, secret,
private URL, raw correspondence, market payload, real price, internal address or runtime
identifier. No provider endpoint was called by BigBrain and no broker/order capability was
introduced.

## Verification

- `dotnet restore BigBrain.slnx` — pass.
- `dotnet build BigBrain.slnx --configuration Release --no-restore` — pass, zero warnings/errors.
- Focused Finance observation read-model tests — pass, 7/7 API tests; Sentinel had no
  matching filtered tests.
- `dotnet test BigBrain.slnx --configuration Release --no-build --no-restore` — pass,
  351 API and 32 Sentinel tests, 383/383 total.
- `npm test -- --run` — pass, 106/106 Web tests.
- `npm run build` — pass.
- `node scripts/verify-documentation.mjs` — pass, 128 Markdown files and 75 unique BB IDs
  (required unsandboxed rerun after sandbox `spawnSync git EPERM`).
- `docker compose config --quiet` — pass.
- `git diff --check` — pass.
- Deployment/runtime provider smoke — not run because no source passed and no provider was
  activated. The read-model wording correction is implemented/tested but not deployed.

## Remaining work

- Send the exact Alpaca Basic/free IEX inquiry already prepared in
  `docs/architecture/finance/provider-retention-inquiry.md`.
- Send a narrowed EODHD Free Starter inquiry only if Alpaca cannot clear the gate.
- Re-evaluate an exact Nasdaq Data Link dataset only when a named free equity/ETF OHLCV
  product and its license are identified.
- Keep Twelve Data Personal inactive while the external budget remains 0 SEK.

## Resumption

Next safe step: obtain written Alpaca Basic/free IEX answers for every unresolved artifact
class. If and only if the exact zero-cost product passes, use this prompt's conditional
owner approval for **FIRST AUTHORIZED MARKET DATA INGESTION**. Otherwise remain fail closed.
