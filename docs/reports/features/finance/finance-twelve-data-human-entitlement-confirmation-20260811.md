# Finance Twelve Data human entitlement confirmation

## Metadata

- Date received and reviewed: 2026-08-11
- Scope: BB-071 entitlement evidence for the described private, self-hosted BigBrain use
- Provider evidence: direct written correspondence from Liam, Twelve Data
- Previous state: **STATE B — HUMAN CONFIRMATION REQUIRED**
- Result: **ENTITLEMENT CLEARED FOR A QUALIFYING PAID PERSONAL PLAN**
- Related commit: assigned on publication

## Status

The human response closes the identified Twelve Data uncertainty for the use case that was
actually submitted. A qualifying Twelve Data Personal plan supports that described private
use. Basic does not: Twelve Data characterized Basic as evaluation access limited to trial
symbols and said a Personal plan is required.

This is licensing/entitlement evidence only. Twelve Data is not active or selected as the
production provider. No plan has been purchased, no account or credential has been created,
no adapter exists and no real market data has been ingested.

## Evidence

The evidence is direct written provider correspondence received and reviewed on 2026-08-11
from Liam at Twelve Data after the detailed inquiry context below. It is private
correspondence, not public documentation, and no public URL is claimed.

## Changes

This slice updates BB-071, the Finance provider-selection and entitlement matrix, current
status/roadmap/module documentation, ADR 0021's evidence note, the inquiry record and report
catalogs. It changes no source code, runtime, account, provider configuration or data.

## Inquiry context

Twelve Data was asked to assess BigBrain as a private, self-hosted personal project with no
operating company, paying subscribers, redistribution, customer use or third-party access.
The described scope was initial US equity/ETF research with local historical memory, stored
raw observations and normalized copies, charts, indicators/features, deterministic replay,
backtesting, strategy research/evaluation, forward/shadow testing, quality/gap/session
analysis, provenance/audit metadata and possible future investment decisions involving only
the owner's own personal funds.

The answer must not be generalized beyond that context.

## Human response and entitlement interpretation

The representative confirmed that a personal project may use a Personal plan and that the
described data may be stored and retained locally for testing and research. The response
also expressly permits retention after termination, including derived data and audit
metadata, and permits using the data for investment of the owner's own funds on a Personal
plan.

| Intended use | Decision for qualifying Personal plan |
| --- | --- |
| Private self-hosted personal use | **SUPPORTED** |
| Local storage and normalized copies | **SUPPORTED** |
| Local historical retention | **SUPPORTED** |
| Testing, research, replay, backtesting and described forward/shadow evaluation | **SUPPORTED within the submitted use case** |
| Post-termination retention | **SUPPORTED** |
| Derived-data retention | **SUPPORTED** |
| Provenance/audit-metadata retention | **SUPPORTED** |
| Owner investing only the owner's personal funds | **SUPPORTED as data use; not trading authorization** |
| Redistribution or third-party/customer access | **NOT AUTHORIZED / outside scope** |
| Commercial operation or paying subscribers | **NOT COVERED**; renewed provider review and a Business plan are required |
| Basic/free operational use | **NOT AUTHORIZED**; Basic is evaluation/trial-symbol access |

Unknown markets, products, exchange-specific scope, customer funds, business use or any
materially different use case must fail closed and reopen entitlement review.

## Provider-selection consequence

Twelve Data is now an **entitlement-cleared paid fallback / qualified candidate** for the
submitted personal scope. It is not a free authorized source and not the selected provider.
BigBrain applies the cost order: free, local/open-source, existing BigBrain infrastructure,
then paid only after verified need. The response therefore removes a legal uncertainty but
does not justify buying or implementing Twelve Data while technically adequate zero-cost
alternatives remain under investigation.

The next research candidate is Alpaca Basic/free IEX market data for US equities. Its exact
personal use, storage, raw/normalized retention, replay/backtest, historical accumulation,
backup/revision, derived/audit, termination, personal-funds and IEX/exchange conditions are
unresolved. No inquiry has been sent and no Alpaca account, key, SDK or adapter exists.

## Trading safety consequence

Finance remains **RESEARCH**. The provider statement about investing the owner's own funds
is market-data licensing evidence, not BigBrain product-owner approval for PAPER or LIVE
trading. There is no broker, order path, executor or AUTO authority. Real-money order
placement still requires a separate explicit future product-owner approval after the
documented research, backtest, paper and limited-live gates.

## Security and privacy

Detta är en sanerad GitHub-version. It records the date, provider, representative name,
relevant statements as careful paraphrases, assessed use-case context and resulting gates.
It omits private email metadata, message IDs, headers, addresses and unrelated personal
information. Direct provider correspondence has no fabricated public URL.

## Verification

- `node scripts/verify-documentation.mjs` — pass, 127 Markdown files and 74 unique BB IDs.
- `git diff --check` — pass.
- `docker compose config --quiet` — pass.
- Source build/test suites — not run; production source and runtime behavior are unchanged.

This documentation-only slice performs no provider or runtime operation.

## Remaining work

- Resolve Alpaca Basic/free IEX entitlement using the published unsent inquiry template.
- Compare any cleared, technically adequate zero-cost option with the Twelve Data paid
  fallback.
- Obtain explicit product-owner provider selection before any account, credential, adapter
  or first authorized ingestion.

## Resumption

Resume from `docs/architecture/finance/market-data-provider-selection.md`,
`docs/architecture/finance/provider-retention-inquiry.md` and BB-071 in
`docs/BACKLOG.md`. The next safe gate is **RESOLVE ALPACA FREE/IEX ENTITLEMENT**, not
Twelve Data implementation.
