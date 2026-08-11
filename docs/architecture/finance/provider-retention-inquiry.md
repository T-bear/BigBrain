# BB-071 provider retention inquiry

Status: ready to send to Twelve Data support/licensing; no provider account or credential is required to send it.
Date prepared: 2026-08-10; narrowed to Twelve Data Basic / US on 2026-08-11.

## Exact Twelve Data Basic inquiry

Send to Twelve Data support/licensing using the public contact channel. Retain the complete
dated response privately and publish only a sanitized entitlement decision.

Subject: Twelve Data Basic — private US data retention and forward-research rights

Hello,

I am in Sweden and am considering the **Twelve Data Basic (Free), individual plan** for a
private, personal, non-commercial and non-display research installation. The proposed first
experiment covers eight US-listed equities/ETFs, uses approximately 15-minute REST polling
during the regular session plus next-day EOD reconciliation, and makes no data available to
third parties. There is no client service, public display, redistribution, broker connection,
order execution, paper trading or high-frequency trading.

Before I create an account, please answer each question specifically for Basic and the
default US equities/ETF feed, and identify the governing terms/documentation date:

1. May raw historical EOD and current/intraday observations be stored in a private local
   database while the Basic subscription/account remains active? What maximum cache or
   retention period applies?
2. May those stored observations be normalized into a provider-neutral schema and retained
   with event, provider, received and first-usable timestamps plus source/provenance?
3. May they be replayed repeatedly for deterministic personal non-display backtests and used
   for prospective forward testing of non-trading strategy rules?
4. May the system create immutable hypothetical “shadow” predictions before outcomes are
   known, later attach actual outcomes, and retain that evidence for comparison?
5. May it compute and retain non-reversible indicators, features, returns, volatility,
   calibration and aggregate strategy-performance metrics while Basic remains active?
6. After termination or account expiry, does section 16.2 require deletion of raw provider
   observations, normalized copies, backups and reproducible dataset revisions within 30
   days? Please answer yes/no for each class.
7. After termination, may non-reversible derived features, aggregate metrics, shadow
   predictions/outcomes and strategy evidence be retained indefinitely? If only some may
   remain, please identify them.
8. May minimal audit metadata that contains no price/volume values remain after deletion—for
   example provider/product, policy version, acquisition timestamps, checksums and deletion
   receipts—or is this outside the section 16.2 compliance-audit exception?
9. Do any exchange, venue or third-party restrictions on the default Basic US feed change
   these answers or require an addendum, registration, fee, attribution or deletion rule?
10. Does the proposed automated internal research require a paid plan, non-display license
    or written addendum despite being personal, non-commercial and non-redistributed?

Please provide a yes/no answer with qualifications for each numbered item. If Basic does not
permit the scope, please identify the lowest-cost exact product/addendum that does.

Thank you.

Live/current scope extension prepared 2026-08-11: ask the exact free product whether
personal/internal non-display forward testing may persist each received observation with
event/provider/received/knowledge timestamps, compute and retain immutable hypothetical
signals/outcomes and derived calibration/performance metrics, and accumulate that prospective
history while subscribed. Require exact feed/venue coverage, delay classification,
third-party restrictions, raw/derived/backup retention periods and termination deletion
duties. A general “algorithmic use” or “internal use” answer is insufficient unless it maps
these artifacts and the intended US/Nordic markets explicitly.

## Intended use

I operate a private, personal software installation in Sweden. I am evaluating a market-
data subscription for a small owner-selected set of Swedish/Nordic and US equities/ETFs.
The initial scope is historical end-of-day OHLCV plus corporate actions needed to derive
adjusted views. There is no redistribution, resale, third-party display, client service,
broker connection, order execution or live trading in this request.

The software would download data into a private local archive, normalize it into a
provider-neutral model and use versioned snapshots repeatedly for deterministic historical
backtests. It would retain derived indicators, aggregate metrics, strategy evidence and
reports. Raw observations and corporate actions are kept separately so a historical result
can be reproduced and provider corrections can create traceable revisions.

## Reusable ready-to-send inquiry

Subject: Personal EOD storage, backtesting and retention rights in Sweden

Hello,

I am considering **[provider and exact plan/product]** for the private, non-commercial use
described above. Before creating an account or integration, please answer each question for
that exact plan and for **Nasdaq Stockholm/other included Nordic venues and US equities/ETFs**:

1. May historical EOD OHLCV obtained under this personal plan be stored in a private local
   database while the subscription is active?
2. Is there a maximum caching or storage period? If yes, what is it for each dataset?
3. May the stored data be reused repeatedly for private deterministic historical
   backtesting and strategy research (non-display use)?
4. May splits, dividends and other supplied corporate-action data be stored with the raw
   historical dataset and used to derive reproducible adjusted views?
5. May non-reversible derived indicators, aggregate metrics, backtest reports and strategy
   evidence be retained indefinitely, including after cancellation?
6. After cancellation or expiry, may previously downloaded raw historical data and
   corporate actions remain stored and be used privately? If not, exactly which raw,
   normalized, backup, audit or derived records must be deleted, and by what deadline?
7. Are Swedish/Nordic exchange datasets subject to additional storage, non-display,
   exchange-agreement, reporting or fee requirements? Please identify them.
8. Does this use require a commercial/professional/non-display license or written addendum
   even though it is personal, manages no third-party assets and redistributes nothing?
9. If the standard plan is insufficient, which product/addendum permits this scope and
   what approximate recurring or exchange fees apply?

Please reference the governing terms, product/tier and effective date in the response.
A yes/no answer with qualifications for each item would be especially helpful.

Thank you.

## Provider-specific notes

- **EODHD Free Starter (first conditional lead):** ask whether deterministic private
  backtesting and each derived-artifact class are covered by “manipulate and analyze”,
  confirm whether the one-month deletion duty covers normalized, derived, audit and backup
  copies, and identify exact US/XSTO free-tier corporate-action/symbol-history scope.
- **Twelve Data Basic (Nordic technical lead):** ask support/sales to reconcile the
  requested archive
  with Terms sections 2.2, 2.3(g), 16.1 and 16.2, and identify the plan/add-on governing
  XSTO/XOSL EOD, corporate actions and non-display backtesting.
- **Tiingo:** ask whether written approval under Terms section 1.6 can cover backtest-
  derived evidence and whether a separate agreement can permit durable raw retention.
- **Massive:** ask whether an individual or business non-display license can override the
  display-only/derived-work restriction and termination deletion duty for US EOD research.

## Evidence handling

The product owner should retain the provider's complete dated response and applicable
terms privately, then publish only a sanitized decision summary in the repository. Do not
commit account identifiers, personal correspondence headers, credentials or private
pricing. BB-071 remains open until the response identifies an adequate exact entitlement;
silence or an informal marketing statement is not approval.
