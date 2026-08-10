# BB-071 provider retention inquiry

Status: ready for product-owner delivery; no provider account or credential is required.
Date prepared: 2026-08-10.

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

- **Twelve Data (first contact):** ask support/sales to reconcile the requested archive
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
